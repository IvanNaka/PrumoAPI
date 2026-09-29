using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T06 — visão geral do portfólio ativo (UC3, passo 5: lista de projetos e de critérios).
    public class PortfolioOverviewTests : ApiTestBase
    {
        public PortfolioOverviewTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Membro_VeProjetosECriteriosDoPortfolio()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var portfolio = await PortfolioAsync(owner);
            await Factory.WithDbAsync(async db =>
            {
                db.PriorityCriterias.Add(new PriorityCriteria { Name = "Valor", ValueWeight = 3, PortfolioId = portfolio.Id, UserId = owner.Id });
                db.Projects.Add(new Project { Name = "Projeto A", PortfolioId = portfolio.Id, OwnerId = owner.Id });
                await db.SaveChangesAsync();
            });
            var client = Factory.ClientFor(owner);

            var criterios = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/criterios");
            var projetos = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/projetos");

            Assert.Equal("Valor", criterios[0].GetProperty("nome").GetString());
            Assert.Equal("Projeto A", projetos[0].GetProperty("nome").GetString());
        }

        [Fact]
        public async Task NaoMembro_Recebe403ComRN06()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var outsider = await UserAsync(RoleName.TechLead);
            var portfolio = await PortfolioAsync(owner);

            var response = await Factory.ClientFor(outsider).GetAsync($"/api/portfolios/{portfolio.Id}/projetos");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Você não tem permissão para acessar este portfólio.", await DetailAsync(response));
        }
    }
}
