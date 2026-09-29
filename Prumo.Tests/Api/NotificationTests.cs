using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T21 — regras, fila e central de notificações (RF45, RF46, Figura 30).
    public class NotificationTests : ApiTestBase
    {
        public NotificationTests(ApiFactory factory) : base(factory) { }

        private async Task<int> ExecutarRegrasAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<INotificationRulesService>().ExecuteAsync();
        }

        /// <summary>Espera o dispatcher deixar as notificações do usuário como Enviada.</summary>
        private async Task<JsonElement> NotificacoesAsync(User user, int esperadas)
        {
            var client = Factory.ClientFor(user);
            for (var i = 0; i < 50; i++)
            {
                var lista = await client.GetFromJsonAsync<JsonElement>("/api/notificacoes");
                if (lista.GetArrayLength() >= esperadas)
                {
                    return lista;
                }

                await Task.Delay(100);
            }

            return await client.GetFromJsonAsync<JsonElement>("/api/notificacoes");
        }

        private static string[] Tipos(JsonElement lista) =>
            lista.EnumerateArray().Select(n => n.GetProperty("tipo").GetString()!).OrderBy(t => t).ToArray();

        [Fact]
        public async Task Regras_GeramOs5Tipos_ParaOsDestinatariosCertos_SemDuplicar()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var po = await UserAsync(RoleName.ProductOwner);
            var tl = await UserAsync(RoleName.TechLead);
            var diretoria = await UserAsync(RoleName.Diretoria);
            var portfolio = await PortfolioAsync(gerente, PortfolioStatus.Monitoramento, po, tl);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var email = $"{Guid.NewGuid():N}@empresa.com";

            var projeto = new Project
            {
                Name = "Atrasado", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmRisco,
                StartDate = hoje.AddDays(-120), EndDate = hoje.AddDays(-5), ApprovedBudget = 1_000,
            };
            var team = new Team { Name = "Squad " + Guid.NewGuid().ToString("N")[..4], PortfolioId = portfolio.Id };
            team.Members.Add(new TeamUser { TeamId = team.Id, Name = "Dev", Email = email, HourlyCost = 0, MonthlyCapacityHours = 10 });
            await Factory.WithDbAsync(async db =>
            {
                db.Projects.Add(projeto);
                db.BudgetExpenses.Add(new BudgetExpense { ProjectId = projeto.Id, Description = "Serviço", Amount = 2_000, Date = hoje.AddDays(-10) });
                db.Teams.Add(team);
                db.Issues.Add(new ExternalIssue
                {
                    ProjectId = projeto.Id, ExternalId = "N-" + Guid.NewGuid().ToString("N")[..6], Title = "Tarefa", Type = ExternalIssueType.Task,
                    Status = "Em andamento", CreatedAt = DateTime.UtcNow.AddDays(-30), DueDate = hoje.AddDays(-2), EstimateHours = 20,
                    AssigneeEmail = email, UpdatedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            });

            var criadas = await ExecutarRegrasAsync();

            Assert.True(criadas >= 6);
            Assert.Equal(new[] { "Atraso", "EstouroOrcamento", "Risco" }, Tipos(await NotificacoesAsync(gerente, 3)));
            Assert.Equal(new[] { "Desalinhamento" }, Tipos(await NotificacoesAsync(po, 1)));
            Assert.Equal(new[] { "Conflito" }, Tipos(await NotificacoesAsync(tl, 1)));
            Assert.Contains("EstouroOrcamento", Tipos(await NotificacoesAsync(diretoria, 1)));

            var conflito = (await NotificacoesAsync(tl, 1))[0];
            Assert.Equal($"A equipe {team.Name} está com ocupação de 200%.", conflito.GetProperty("mensagem").GetString());
            Assert.Equal("Enviada", conflito.GetProperty("status").GetString());
            Assert.Equal("Equipe", conflito.GetProperty("entidadeTipo").GetString());

            // Rodar de novo nas próximas 24 h não duplica.
            Assert.Equal(0, await ExecutarRegrasAsync());
            Assert.Equal(3, (await NotificacoesAsync(gerente, 3)).GetArrayLength());
        }

        [Fact]
        public async Task CicloDeVida_LerArquivar_SomenteAsProprias()
        {
            var user = await UserAsync(RoleName.Desenvolvedor);
            var outro = await UserAsync(RoleName.Desenvolvedor);
            using (var scope = Factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                Assert.True(await service.CreateAsync(user.Id, AlertType.Risco, "Teste", "Projeto", Guid.NewGuid()));
            }

            var client = Factory.ClientFor(user);
            var item = (await NotificacoesAsync(user, 1))[0];
            var id = item.GetProperty("id").GetGuid();
            Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/notificacoes/nao-lidas/contagem")).GetProperty("quantidade").GetInt32());

            var arquivarAntes = await client.PatchAsync($"/api/notificacoes/{id}/arquivar", null);
            Assert.Equal(HttpStatusCode.Conflict, arquivarAntes.StatusCode); // Enviada não arquiva direto (Figura 30)

            var deOutro = await Factory.ClientFor(outro).PatchAsync($"/api/notificacoes/{id}/lida", null);
            Assert.Equal(HttpStatusCode.NotFound, deOutro.StatusCode);

            var lida = await client.PatchAsync($"/api/notificacoes/{id}/lida", null);
            Assert.Equal("Lida", (await lida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
            Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/notificacoes/nao-lidas/contagem")).GetProperty("quantidade").GetInt32());

            var arquivada = await client.PatchAsync($"/api/notificacoes/{id}/arquivar", null);
            Assert.Equal("Arquivada", (await arquivada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
            Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/notificacoes")).GetArrayLength());
            Assert.Equal(1, (await client.GetFromJsonAsync<JsonElement>("/api/notificacoes?status=Arquivada")).GetArrayLength());
        }

        [Fact]
        public async Task JobDiario_Enviada7DiasViraIgnorada_E30DiasViraArquivada()
        {
            var user = await UserAsync(RoleName.QA);
            var agora = DateTime.UtcNow;
            var enviada = new Alert { UserId = user.Id, Type = AlertType.Atraso, Message = "a", Status = AlertStatus.Enviada, SentAt = agora.AddDays(-8) };
            var recente = new Alert { UserId = user.Id, Type = AlertType.Atraso, Message = "b", Status = AlertStatus.Enviada, SentAt = agora.AddDays(-2) };
            var lida = new Alert { UserId = user.Id, Type = AlertType.Risco, Message = "c", Status = AlertStatus.Lida, SentAt = agora.AddDays(-40), ReadAt = agora.AddDays(-31) };
            await Factory.WithDbAsync(async db => { db.Alerts.AddRange(enviada, recente, lida); await db.SaveChangesAsync(); });

            using (var scope = Factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<INotificationService>().ExpireAsync(agora);
            }

            var status = await Factory.WithDbAsync(db => db.Alerts.AsNoTracking().Where(a => a.UserId == user.Id).ToDictionaryAsync(a => a.Message, a => a.Status));
            Assert.Equal(AlertStatus.Ignorada, status["a"]);
            Assert.Equal(AlertStatus.Enviada, status["b"]);
            Assert.Equal(AlertStatus.Arquivada, status["c"]);
        }
    }
}
