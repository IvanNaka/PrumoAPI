using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T17 — sincronização de issues e worklogs (RF47, RF51, UC16, Seção 3.4.4).
    public abstract class JiraSyncTestBase : ApiTestBase
    {
        protected JiraSyncTestBase(ApiFactory factory) : base(factory) { }

        protected ApiFactory.FakeJiraProvider Fake => Factory.Services.GetRequiredService<ApiFactory.FakeJiraProvider>();

        protected async Task<HttpClient> ConectarAsync()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);
            var client = Factory.ClientFor(dev);
            var put = await client.PutAsJsonAsync("/api/integracoes/jira", new
            {
                url = "https://empresa.atlassian.net", email = "jira@empresa.com",
                apiToken = ApiFactory.FakeJiraProvider.TokenValido, intervaloSincronizacaoMinutos = 30, ativo = true,
            });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            return client;
        }

        protected async Task<Guid> ProjetoAsync(string jiraKey)
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente, PortfolioStatus.Monitoramento);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = new Project
            {
                Name = "Projeto " + jiraKey, PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                StartDate = hoje.AddDays(-90), EndDate = hoje.AddDays(180), ApprovedBudget = 100_000, JiraProjectKey = jiraKey,
            };
            await Factory.WithDbAsync(async db => { db.Projects.Add(project); await db.SaveChangesAsync(); });
            return project.Id;
        }

        /// <summary>POST /sincronizar (202) e espera o status sair de Sincronizando.</summary>
        protected static async Task<JsonElement> SincronizarAsync(HttpClient client)
        {
            var post = await client.PostAsync("/api/integracoes/jira/sincronizar", null);
            Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);
            var body = await post.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Sincronizando", body.GetProperty("status").GetString());

            for (var i = 0; i < 100; i++)
            {
                var atual = await client.GetFromJsonAsync<JsonElement>("/api/integracoes/jira");
                if (atual.GetProperty("status").GetString() != "Sincronizando")
                {
                    return atual;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException("A sincronização não terminou.");
        }

        protected static ExternalIssueData Issue(string key, ExternalIssueType type, DateTime updated, bool done = false, string? assignee = "dev@empresa.com") =>
            new(key, "Issue " + key, type, done ? "Concluído" : "Em andamento", done,
                updated.AddDays(-10), done ? updated : null, DateOnly.FromDateTime(updated).AddDays(5), updated, 8m, assignee);
    }

    public class JiraSyncTests : JiraSyncTestBase
    {
        public JiraSyncTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task SincronizaIssuesDos4TiposEWorklogs_SemDuplicarNaSegundaVez()
        {
            var client = await ConectarAsync();
            var projectId = await ProjetoAsync("PRU");
            var t0 = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
            Fake.Issues["PRU"] = new List<ExternalIssueData>
            {
                Issue("PRU-1", ExternalIssueType.Story, t0, done: true),
                Issue("PRU-2", ExternalIssueType.Task, t0),
                Issue("PRU-3", ExternalIssueType.Feature, t0),
                Issue("PRU-4", ExternalIssueType.Bug, t0, done: true),
            };
            Fake.Worklogs["PRU-1"] = new List<ExternalWorklogData>
            {
                new("1001", "dev@empresa.com", 2.5m, new DateOnly(2026, 9, 2)),
                new("1002", "qa@empresa.com", 1.5m, new DateOnly(2026, 9, 3)),
            };
            Fake.Worklogs["PRU-4"] = new List<ExternalWorklogData> { new("1003", "dev@empresa.com", 3m, new DateOnly(2026, 9, 4)) };

            var depois = await SincronizarAsync(client);

            Assert.Equal("Conectada", depois.GetProperty("status").GetString());
            Assert.NotEqual(JsonValueKind.Null, depois.GetProperty("ultimaSincronizacao").ValueKind);
            var issues = await Factory.WithDbAsync(db => db.Issues.AsNoTracking().Where(i => i.ProjectId == projectId).ToListAsync());
            Assert.Equal(4, issues.Count);
            Assert.Equal(new[] { ExternalIssueType.Story, ExternalIssueType.Task, ExternalIssueType.Feature, ExternalIssueType.Bug }.OrderBy(t => t),
                issues.Select(i => i.Type).OrderBy(t => t));
            Assert.Equal(4m, issues.Single(i => i.ExternalId == "PRU-1").SpentHours);
            var logs = await client.GetFromJsonAsync<JsonElement>("/api/integracoes/jira/logs");
            Assert.True(logs[0].GetProperty("sucesso").GetBoolean());
            Assert.Equal(4, logs[0].GetProperty("issuesProcessadas").GetInt32());
            Assert.Equal(3, logs[0].GetProperty("worklogsProcessados").GetInt32());

            // Segunda sincronização: upsert por IdExterno, só a issue alterada busca worklogs de novo.
            var consultas = Fake.WorklogsConsultados.Count;
            Fake.Issues["PRU"][0] = Issue("PRU-1", ExternalIssueType.Story, t0.AddHours(1), done: true);
            Fake.Worklogs["PRU-1"] = new List<ExternalWorklogData> { new("1001", "dev@empresa.com", 5m, new DateOnly(2026, 9, 2)) };

            await SincronizarAsync(client);

            Assert.Equal(consultas + 1, Fake.WorklogsConsultados.Count);
            Assert.Equal(4, await Factory.WithDbAsync(db => db.Issues.CountAsync(i => i.ProjectId == projectId)));
            var worklogs = await Factory.WithDbAsync(db => db.Worklogs.AsNoTracking().Where(w => w.Issue.ProjectId == projectId).ToListAsync());
            Assert.Equal(2, worklogs.Count); // 1002 foi apagado no Jira
            Assert.Equal(5m, worklogs.Single(w => w.ExternalId == "1001").Hours);
            Assert.Equal(5m, await Factory.WithDbAsync(db => db.Issues.Where(i => i.ExternalId == "PRU-1").Select(i => i.SpentHours).SingleAsync()));
        }

        [Fact]
        public async Task IntervaloConfiguradoERespeitado()
        {
            await ConectarAsync();
            using var scope = Factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IIntegrationSyncService>();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var integration = await db.Integrations.SingleAsync(i => i.Type == IntegrationType.Jira);
            var ultima = DateTime.UtcNow;
            integration.LastSyncedAt = ultima;
            await db.SaveChangesAsync();

            Assert.False(await service.IsJiraSyncDueAsync(ultima.AddMinutes(29)));
            Assert.True(await service.IsJiraSyncDueAsync(ultima.AddMinutes(30)));

            integration.IsActive = false;
            await db.SaveChangesAsync();
            Assert.False(await service.IsJiraSyncDueAsync(ultima.AddMinutes(60)));
            integration.IsActive = true;
            await db.SaveChangesAsync();
        }
    }

    public class JiraSyncFailureTests : JiraSyncTestBase
    {
        public JiraSyncFailureTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Falha5xx_NovasTentativasA5MinutosXTentativas_EDepoisDe3_SoNaProximaJanela()
        {
            var client = await ConectarAsync();
            await ProjetoAsync("ERR");
            Fake.Erro = new IntegrationUnavailableException("O Jira respondeu HTTP 503.");

            for (var tentativa = 1; tentativa <= 3; tentativa++)
            {
                var antes = DateTime.UtcNow;
                var depois = await SincronizarAsync(client);

                Assert.Equal("FalhaSincronizacao", depois.GetProperty("status").GetString());
                Assert.Equal(tentativa, depois.GetProperty("tentativasFalhas").GetInt32());
                var proxima = depois.GetProperty("proximaTentativa").GetDateTime();
                var esperado = tentativa < 3 ? 5 * tentativa : 30; // depois de 3 falhas, só no intervalo normal
                Assert.InRange((proxima - antes).TotalMinutes, esperado - 0.1, esperado + 0.5);
            }

            var logs = await client.GetFromJsonAsync<JsonElement>("/api/integracoes/jira/logs");
            Assert.Equal(3, logs.GetArrayLength());
            Assert.StartsWith("API do Jira indisponível. Nova tentativa agendada.", logs[0].GetProperty("mensagemErro").GetString());

            // Sucesso zera as tentativas.
            Fake.Erro = null;
            var ok = await SincronizarAsync(client);
            Assert.Equal("Conectada", ok.GetProperty("status").GetString());
            Assert.Equal(0, ok.GetProperty("tentativasFalhas").GetInt32());

            // 401 -> ErroConexao (RN26); a partir daí não dá para sincronizar sem reconfigurar.
            Fake.Erro = new IntegrationAuthException("401");
            var expirado = await SincronizarAsync(client);
            Assert.Equal("ErroConexao", expirado.GetProperty("status").GetString());
            logs = await client.GetFromJsonAsync<JsonElement>("/api/integracoes/jira/logs");
            Assert.Equal("Token do Jira expirado. Refaça a autenticação.", logs[0].GetProperty("mensagemErro").GetString());
            Assert.False(logs[0].GetProperty("sucesso").GetBoolean());

            var bloqueado = await client.PostAsync("/api/integracoes/jira/sincronizar", null);
            Assert.Equal(HttpStatusCode.Conflict, bloqueado.StatusCode);
            Fake.Erro = null;
        }
    }
}
