using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T11 — associações com OKR (RF16, UC9).
    public class OkrAssociationTests : ApiTestBase
    {
        public OkrAssociationTests(ApiFactory factory) : base(factory) { }

        private async Task<(HttpClient Client, Portfolio Portfolio, Guid ProjectId, Guid OkrId)> SetupAsync()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var portfolio = await PortfolioAsync(po);
            var project = new Project
            {
                Name = "Projeto", PortfolioId = portfolio.Id, OwnerId = po.Id,
                StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            };
            var okr = new Objective { Title = "OKR" };
            okr.KeyResults.Add(new KeyResult { ObjectiveId = okr.Id, Title = "KR", TargetValue = 10, CurrentValue = 5 });
            await Factory.WithDbAsync(async db =>
            {
                db.Projects.Add(project);
                db.Objectives.Add(okr);
                await db.SaveChangesAsync();
            });
            return (Factory.ClientFor(po), portfolio, project.Id, okr.Id);
        }

        [Fact]
        public async Task AssociarEDesassociarProjeto_EhIdempotente()
        {
            var (client, _, projectId, okrId) = await SetupAsync();

            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/projetos/{projectId}/okrs/{okrId}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/projetos/{projectId}/okrs/{okrId}", null)).StatusCode);
            var count = await Factory.WithDbAsync(db => db.ProjectObjectives.CountAsync(po => po.ProjectId == projectId));
            Assert.Equal(1, count);

            var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projectId}");
            Assert.Equal(50m, detalhe.GetProperty("okrs")[0].GetProperty("progresso").GetDecimal());

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projetos/{projectId}/okrs/{okrId}")).StatusCode);
            Assert.Equal(0, await Factory.WithDbAsync(db => db.ProjectObjectives.CountAsync(po => po.ProjectId == projectId)));
        }

        [Fact]
        public async Task OkrInexistente_RN14()
        {
            var (client, portfolio, projectId, _) = await SetupAsync();

            var projeto = await client.PostAsync($"/api/projetos/{projectId}/okrs/{Guid.NewGuid()}", null);
            var portfolioResp = await client.PostAsync($"/api/portfolios/{portfolio.Id}/okrs/{Guid.NewGuid()}", null);

            Assert.Equal(HttpStatusCode.NotFound, projeto.StatusCode);
            Assert.Equal("OKR não encontrado.", await DetailAsync(projeto));
            Assert.Equal(HttpStatusCode.NotFound, portfolioResp.StatusCode);
        }

        [Fact]
        public async Task OkrsDoPortfolio()
        {
            var (client, portfolio, _, okrId) = await SetupAsync();

            await client.PostAsync($"/api/portfolios/{portfolio.Id}/okrs/{okrId}", null);
            var lista = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/okrs");

            Assert.Equal(okrId, lista[0].GetProperty("id").GetGuid());
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/portfolios/{portfolio.Id}/okrs/{okrId}")).StatusCode);
        }
    }
}
