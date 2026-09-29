using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T12 — lançamentos financeiros e Burn Rate (RF22–RF24).
    public class FinanceTests : ApiTestBase
    {
        public FinanceTests(ApiFactory factory) : base(factory) { }

        private async Task<(User Gerente, Guid ProjectId)> SetupAsync()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = new Project
            {
                Name = "Projeto", PortfolioId = portfolio.Id, OwnerId = gerente.Id, Status = ProjectStatus.EmAndamento,
                StartDate = hoje.AddDays(-90), EndDate = hoje.AddDays(180), ApprovedBudget = 100_000,
            };
            await Factory.WithDbAsync(async db => { db.Projects.Add(project); await db.SaveChangesAsync(); });
            return (gerente, project.Id);
        }

        [Fact]
        public async Task Lancamento_ECalculoDoBurnRate()
        {
            var (gerente, projectId) = await SetupAsync();
            var client = Factory.ClientFor(gerente);
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            var created = await client.PostAsJsonAsync($"/api/projetos/{projectId}/lancamentos",
                new { descricao = "Licenças", valor = 30000, tipo = "Custo", dataLancamento = hoje.ToString("yyyy-MM-dd") });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var lista = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projectId}/lancamentos");
            Assert.Single(lista.EnumerateArray());

            var indicadores = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projectId}/indicadores");
            var burn = indicadores.GetProperty("burnRate");
            Assert.True(burn.GetProperty("disponivel").GetBoolean());
            Assert.Equal(30000m, burn.GetProperty("custoRealizado").GetDecimal());
            Assert.Equal(30m, burn.GetProperty("percentualConsumido").GetDecimal());
            Assert.Equal(10000m, burn.GetProperty("burnRateMensal").GetDecimal());
        }

        [Fact]
        public async Task LancamentoComDataFuturaOuValorInvalido_RN28()
        {
            var (gerente, projectId) = await SetupAsync();
            var client = Factory.ClientFor(gerente);
            var amanha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd");

            var futuro = await client.PostAsJsonAsync($"/api/projetos/{projectId}/lancamentos", new { descricao = "X", valor = 10, tipo = "Custo", dataLancamento = amanha });
            var zero = await client.PostAsJsonAsync($"/api/projetos/{projectId}/lancamentos", new { descricao = "X", valor = 0, tipo = "Despesa", dataLancamento = "2026-01-01" });

            Assert.Equal(HttpStatusCode.BadRequest, futuro.StatusCode);
            Assert.Equal("Lançamento inválido: valor deve ser positivo e data não pode ser futura.", await DetailAsync(futuro));
            Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        }

        [Fact]
        public async Task Permissoes_DoFinanceiro()
        {
            var (gerente, projectId) = await SetupAsync();
            var po = await UserAsync(RoleName.ProductOwner);
            var dev = await UserAsync(RoleName.Desenvolvedor);
            await Factory.WithDbAsync(async db =>
            {
                var portfolioId = db.Projects.Single(p => p.Id == projectId).PortfolioId;
                db.PortfolioMembers.Add(new PortfolioMember { PortfolioId = portfolioId, UserId = po.Id });
                db.PortfolioMembers.Add(new PortfolioMember { PortfolioId = portfolioId, UserId = dev.Id });
                await db.SaveChangesAsync();
            });

            Assert.Equal(HttpStatusCode.OK, (await Factory.ClientFor(po).GetAsync($"/api/projetos/{projectId}/lancamentos")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Factory.ClientFor(po).PostAsJsonAsync($"/api/projetos/{projectId}/lancamentos", new { descricao = "X", valor = 1, tipo = "Custo", dataLancamento = "2026-01-01" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Factory.ClientFor(dev).GetAsync($"/api/projetos/{projectId}/lancamentos")).StatusCode);
        }
    }
}
