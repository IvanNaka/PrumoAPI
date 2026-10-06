using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // Alocação de equipes em projetos.
    public class ProjectTeamTests : ApiTestBase
    {
        public ProjectTeamTests(ApiFactory factory) : base(factory) { }

        private async Task<(User Gerente, HttpClient Client, Guid ProjectId, Guid TeamId)> SetupAsync()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            var project = new Project
            {
                Name = "Projeto", PortfolioId = portfolio.Id, OwnerId = gerente.Id,
                StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            };
            var team = new Team { Name = $"Equipe {Guid.NewGuid():N}" };
            team.Members.Add(new TeamUser { TeamId = team.Id, Name = "Ana", Email = $"{Guid.NewGuid():N}@x.com", MonthlyCapacityHours = 120 });
            team.Members.Add(new TeamUser { TeamId = team.Id, Name = "Bia", Email = $"{Guid.NewGuid():N}@x.com", MonthlyCapacityHours = 80 });
            await Factory.WithDbAsync(async db =>
            {
                db.Projects.Add(project);
                db.Teams.Add(team);
                await db.SaveChangesAsync();
            });
            return (gerente, Factory.ClientFor(gerente), project.Id, team.Id);
        }

        [Fact]
        public async Task AlocarEDesalocarEquipe_EhIdempotente()
        {
            var (_, client, projectId, teamId) = await SetupAsync();

            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/projetos/{projectId}/equipes/{teamId}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/projetos/{projectId}/equipes/{teamId}", null)).StatusCode);
            Assert.Equal(1, await Factory.WithDbAsync(db => db.ProjectTeams.CountAsync(pt => pt.ProjectId == projectId)));

            var equipes = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projectId}/equipes");
            Assert.Equal(teamId, equipes[0].GetProperty("id").GetGuid());
            Assert.Equal(2, equipes[0].GetProperty("quantidadeMembros").GetInt32());
            Assert.Equal(200, equipes[0].GetProperty("capacidadeMensalTotal").GetInt32());

            var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projectId}");
            Assert.Equal(teamId, detalhe.GetProperty("equipes")[0].GetProperty("id").GetGuid());

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projetos/{projectId}/equipes/{teamId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projetos/{projectId}/equipes/{teamId}")).StatusCode);
            Assert.Equal(0, await Factory.WithDbAsync(db => db.ProjectTeams.CountAsync(pt => pt.ProjectId == projectId)));
        }

        [Fact]
        public async Task EquipeInexistente_Retorna404()
        {
            var (_, client, projectId, _) = await SetupAsync();

            var response = await client.PostAsync($"/api/projetos/{projectId}/equipes/{Guid.NewGuid()}", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Equipe não encontrada.", await DetailAsync(response));
        }

        [Fact]
        public async Task SemPerfilDeEdicao_Retorna403()
        {
            var (_, _, projectId, teamId) = await SetupAsync();
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).PostAsync($"/api/projetos/{projectId}/equipes/{teamId}", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
