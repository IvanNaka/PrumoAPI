using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T14 — dependências entre projetos (RF26–RF28, UC12).
    public class DependencyApiTests : ApiTestBase
    {
        public DependencyApiTests(ApiFactory factory) : base(factory) { }

        private async Task<(HttpClient Client, Portfolio Portfolio, Guid A, Guid B, Guid C)> SetupAsync()
        {
            var tl = await UserAsync(RoleName.TechLead, RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(tl);
            Project Novo(string nome) => new()
            {
                Name = nome, PortfolioId = portfolio.Id, OwnerId = tl.Id, Status = ProjectStatus.EmAndamento,
                StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            };
            Project a = Novo("A"), b = Novo("B"), c = Novo("C");
            await Factory.WithDbAsync(async db => { db.Projects.AddRange(a, b, c); await db.SaveChangesAsync(); });
            return (Factory.ClientFor(tl), portfolio, a.Id, b.Id, c.Id);
        }

        private static Task<HttpResponseMessage> CriarAsync(HttpClient client, Guid origem, Guid destino) =>
            client.PostAsJsonAsync("/api/dependencias", new { projetoOrigemId = origem, projetoDestinoId = destino, descricao = "precisa da API" });

        [Fact]
        public async Task Ciclo_RN19()
        {
            var (client, _, a, b, c) = await SetupAsync();
            Assert.Equal(HttpStatusCode.Created, (await CriarAsync(client, a, b)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await CriarAsync(client, b, c)).StatusCode);

            var ciclo = await CriarAsync(client, c, a);

            Assert.Equal(HttpStatusCode.Conflict, ciclo.StatusCode);
            Assert.Equal("Dependência circular não permitida.", await DetailAsync(ciclo));
        }

        [Fact]
        public async Task AutoDependencia_RN20_ERepetida_RN21()
        {
            var (client, _, a, b, _) = await SetupAsync();

            var auto = await CriarAsync(client, a, a);
            Assert.Equal(HttpStatusCode.BadRequest, auto.StatusCode);
            Assert.Equal("Um projeto não pode depender de si mesmo.", await DetailAsync(auto));

            await CriarAsync(client, a, b);
            var repetida = await CriarAsync(client, a, b);
            Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
            Assert.Equal("Esta dependência já existe.", await DetailAsync(repetida));
        }

        [Fact]
        public async Task SuspenderB_MarcaAB_ComoEmRisco()
        {
            var (client, portfolio, a, b, _) = await SetupAsync();
            await CriarAsync(client, a, b);

            var antes = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/dependencias");
            Assert.False(antes[0].GetProperty("emRisco").GetBoolean());

            await client.PostAsJsonAsync($"/api/projetos/{b}/status", new { acao = "Suspender" });

            var depois = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/dependencias");
            Assert.True(depois[0].GetProperty("emRisco").GetBoolean());
            Assert.Equal("Projeto dependente está suspenso", depois[0].GetProperty("motivo").GetString());

            var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{a}");
            Assert.True(detalhe.GetProperty("dependencias")[0].GetProperty("emRisco").GetBoolean());
        }

        [Fact]
        public async Task ProductOwnerNaoCadastraDependencia()
        {
            var (_, portfolio, a, b, _) = await SetupAsync();
            var po = await UserAsync(RoleName.ProductOwner);

            var response = await CriarAsync(Factory.ClientFor(po), a, b);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
