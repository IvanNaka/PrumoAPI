using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T19 — GET /portfolios/{id}/dashboard (RF35–RF42, UC11).
    public class DashboardTests : ApiTestBase
    {
        public DashboardTests(ApiFactory factory) : base(factory) { }

        private static readonly string[] Chaves =
        {
            "burnRate", "saude", "alocacaoEstrategica", "qualidade", "capacidade", "leadTime", "vpl", "alinhamentoOkr",
        };

        [Fact]
        public async Task Dashboard_Devolve8Indicadores_ComPeriodoPadraoDe90Dias()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente, PortfolioStatus.Monitoramento);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            await Factory.WithDbAsync(async db =>
            {
                db.Projects.Add(new Project
                {
                    Name = "Projeto", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                    StrategicCategory = StrategicCategory.Grow, StartDate = hoje.AddDays(-30), EndDate = hoje.AddDays(60), ApprovedBudget = 10_000,
                });
                await db.SaveChangesAsync();
            });

            var dashboard = await Factory.ClientFor(gerente).GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/dashboard");

            foreach (var chave in Chaves)
            {
                Assert.True(dashboard.TryGetProperty(chave, out var indicador), chave);
                Assert.True(indicador.TryGetProperty("disponivel", out _), chave);
            }

            Assert.Equal(hoje.AddDays(-90).ToString("yyyy-MM-dd"), dashboard.GetProperty("de").GetString());
            Assert.True(dashboard.GetProperty("alocacaoEstrategica").GetProperty("disponivel").GetBoolean());
            Assert.False(dashboard.GetProperty("leadTime").GetProperty("disponivel").GetBoolean());
            Assert.Equal("Dados insuficientes para calcular este indicador.", dashboard.GetProperty("leadTime").GetProperty("motivo").GetString());

            var um = await Factory.ClientFor(gerente).GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/indicadores/alocacaoEstrategica");
            Assert.Equal(100m, um.GetProperty("categorias").EnumerateArray().Single(c => c.GetProperty("categoria").GetString() == "Grow").GetProperty("percentualOrcamento").GetDecimal());
        }

        [Fact]
        public async Task Dashboard_SemAcessoAoPortfolio_403_EIndicadorInexistente_404()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var outro = await UserAsync(RoleName.Desenvolvedor);
            var portfolio = await PortfolioAsync(gerente);

            var semAcesso = await Factory.ClientFor(outro).GetAsync($"/api/portfolios/{portfolio.Id}/dashboard");
            var inexistente = await Factory.ClientFor(gerente).GetAsync($"/api/portfolios/{portfolio.Id}/indicadores/xyz");
            var periodo = await Factory.ClientFor(gerente).GetAsync($"/api/portfolios/{portfolio.Id}/dashboard?de=2026-05-01&ate=2026-04-01");

            Assert.Equal(HttpStatusCode.Forbidden, semAcesso.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, periodo.StatusCode);
        }
    }
}
