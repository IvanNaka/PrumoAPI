using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T07 — critérios de prioridade (RF07–RF09, UC4–UC6).
    public class CriteriaTests : ApiTestBase
    {
        public CriteriaTests(ApiFactory factory) : base(factory) { }

        private async Task<(User Po, Portfolio Portfolio, HttpClient Client)> SetupAsync()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var portfolio = await PortfolioAsync(po);
            return (po, portfolio, Factory.ClientFor(po));
        }

        [Fact]
        public async Task Criar_ComDescricaoETipo()
        {
            var (_, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios",
                new { nome = "Esforço", descricao = "Horas estimadas", peso = 2, tipo = "Custo" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Custo", body.GetProperty("tipo").GetString());
            Assert.Equal("Horas estimadas", body.GetProperty("descricao").GetString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10.01)]
        public async Task PesoForaDoIntervalo_RN07(decimal peso)
        {
            var (_, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "X", peso, tipo = "Beneficio" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Peso inválido. Informe um valor maior que 0 e até 10.", await DetailAsync(response));
        }

        [Fact]
        public async Task PesoDez_EhValido()
        {
            var (_, portfolio, client) = await SetupAsync();
            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Máximo", peso = 10 });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task NomeVazio_RN08()
        {
            var (_, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "  ", peso = 3 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("O nome do critério é obrigatório.", await DetailAsync(response));
        }

        [Fact]
        public async Task NomeRepetido_RN09()
        {
            var (_, portfolio, client) = await SetupAsync();
            await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Risco", peso = 3 });

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "risco", peso = 2 });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Já existe um critério com este nome neste portfólio.", await DetailAsync(response));
        }

        [Fact]
        public async Task CriterioInexistente_RN10()
        {
            var (_, _, client) = await SetupAsync();

            var response = await client.PutAsJsonAsync($"/api/criterios/{Guid.NewGuid()}", new { nome = "X", peso = 3 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Critério não encontrado.", await DetailAsync(response));
        }

        [Fact]
        public async Task ExcluirCriterioComNotas_RN11()
        {
            var (po, portfolio, client) = await SetupAsync();
            var criterio = await (await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Valor", peso = 4 }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var criterioId = criterio.GetProperty("id").GetGuid();
            await Factory.WithDbAsync(async db =>
            {
                var project = new Project { Name = "P", PortfolioId = portfolio.Id, OwnerId = po.Id };
                db.Projects.Add(project);
                db.ProjectEvaluations.Add(new ProjectEvaluation { ProjectId = project.Id, PriorityCriteriaId = criterioId, UserId = po.Id, Score = 3 });
                await db.SaveChangesAsync();
            });

            var response = await client.DeleteAsync($"/api/criterios/{criterioId}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Critério associado a avaliações não pode ser excluído.", await DetailAsync(response));
        }

        [Fact]
        public async Task ExcluirCriterioSemNotas_Funciona()
        {
            var (_, portfolio, client) = await SetupAsync();
            var criterio = await (await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Temp", peso = 1 }))
                .Content.ReadFromJsonAsync<JsonElement>();

            var response = await client.DeleteAsync($"/api/criterios/{criterio.GetProperty("id").GetGuid()}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CriterioNovo_DeixaProjetosPriorizadosComoReavaliado()
        {
            var (po, portfolio, client) = await SetupAsync();
            var projectId = Guid.Empty;
            await Factory.WithDbAsync(async db =>
            {
                var priorizado = new Project { Name = "Priorizado", PortfolioId = portfolio.Id, OwnerId = po.Id, EvaluationStatus = EvaluationStatus.Priorizado, CurrentScore = 70 };
                var aprovado = new Project { Name = "Aprovado", PortfolioId = portfolio.Id, OwnerId = po.Id, EvaluationStatus = EvaluationStatus.Aprovado, CurrentScore = 60 };
                db.Projects.AddRange(priorizado, aprovado);
                projectId = priorizado.Id;
                await db.SaveChangesAsync();
            });

            await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Novo critério", peso = 3 });

            var statuses = await Factory.WithDbAsync(db => db.Projects.AsNoTracking()
                .Where(p => p.PortfolioId == portfolio.Id).Select(p => p.EvaluationStatus).ToListAsync());
            Assert.All(statuses, s => Assert.Equal(EvaluationStatus.Reavaliado, s));
        }

        [Fact]
        public async Task GerenteNaoEditaCriterios_RN27()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);

            var response = await Factory.ClientFor(gerente).PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "X", peso = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Seu perfil não tem permissão para esta ação.", await DetailAsync(response));
        }
    }
}
