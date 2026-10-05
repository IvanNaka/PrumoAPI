using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T13 — business case e VPL (RF25, RF41).
    public class BusinessCaseTests : ApiTestBase
    {
        public BusinessCaseTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task BusinessCase_SubstituiFluxos_ECalculaVpl()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            var project = new Project
            {
                Name = "P", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), ApprovedBudget = 10000,
            };
            await Factory.WithDbAsync(async db => { db.Projects.Add(project); await db.SaveChangesAsync(); });
            var client = Factory.ClientFor(gerente);

            var semBc = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{project.Id}/indicadores");
            Assert.False(semBc.GetProperty("vpl").GetProperty("disponivel").GetBoolean());

            await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 1000, taxaDescontoAnual = 12.6825,
                fluxosPrevistos = new[] { new { mes = 1, valor = 100 } },
            });
            var put = await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 1000, taxaDescontoAnual = 12.6825,
                fluxosPrevistos = new[] { new { mes = 1, valor = 600 }, new { mes = 2, valor = 600 } },
            });
            var bc = await put.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(put.IsSuccessStatusCode, bc.ToString());
            Assert.Equal(2, bc.GetProperty("fluxosPrevistos").GetArrayLength());

            var retorno = await client.PostAsJsonAsync($"/api/projetos/{project.Id}/retornos", new { data = "2026-02-01", valor = 500, descricao = "Economia" });
            Assert.Equal(HttpStatusCode.Created, retorno.StatusCode);

            var indicadores = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{project.Id}/indicadores");
            var vpl = indicadores.GetProperty("vpl");
            Assert.True(vpl.GetProperty("disponivel").GetBoolean());
            Assert.Equal(182.24m, vpl.GetProperty("vplEsperado").GetDecimal());
        }

        [Fact]
        public async Task TaxaForaDoIntervalo_Recebe400()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            var project = new Project { Name = "P", PortfolioId = portfolio.Id, OwnerId = gerente.Id };
            await Factory.WithDbAsync(async db => { db.Projects.Add(project); await db.SaveChangesAsync(); });

            var response = await Factory.ClientFor(gerente).PutAsJsonAsync($"/api/projetos/{project.Id}/business-case",
                new { investimentoInicial = 10, taxaDescontoAnual = 101 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FluxoPrevistoNegativoOuAcimaDoLimite_Recebe400()
        {
            var (client, project) = await ProjetoAsync();

            var negativo = await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 1000, taxaDescontoAnual = 10,
                fluxosPrevistos = new[] { new { mes = 1, valor = -100 } },
            });
            var gigante = await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 1000, taxaDescontoAnual = 10,
                fluxosPrevistos = new[] { new { mes = 1, valor = 1_000_000_000_001m } },
            });
            var investimentoGigante = await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 1_000_000_000_001m, taxaDescontoAnual = 10,
            });

            Assert.Equal(HttpStatusCode.BadRequest, negativo.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, gigante.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, investimentoGigante.StatusCode);
        }

        [Fact]
        public async Task FluxoPrevistoZero_EhAceito()
        {
            var (client, project) = await ProjetoAsync();

            var response = await client.PutAsJsonAsync($"/api/projetos/{project.Id}/business-case", new
            {
                investimentoInicial = 0, taxaDescontoAnual = 0,
                fluxosPrevistos = new[] { new { mes = 1, valor = 0 } },
            });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        [InlineData(1_000_000_000_001)]
        public async Task RetornoComValorInvalido_Recebe400(double valor)
        {
            var (client, project) = await ProjetoAsync();

            var response = await client.PostAsJsonAsync($"/api/projetos/{project.Id}/retornos",
                new { data = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), valor = (decimal)valor });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RetornoComDataFutura_Recebe400()
        {
            var (client, project) = await ProjetoAsync();

            var amanha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var response = await client.PostAsJsonAsync($"/api/projetos/{project.Id}/retornos",
                new { data = amanha.ToString("yyyy-MM-dd"), valor = 100 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<(HttpClient Client, Project Project)> ProjetoAsync()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            var project = new Project
            {
                Name = "P", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31), ApprovedBudget = 10000,
            };
            await Factory.WithDbAsync(async db => { db.Projects.Add(project); await db.SaveChangesAsync(); });
            return (Factory.ClientFor(gerente), project);
        }
    }
}
