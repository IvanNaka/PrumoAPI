using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T08 — projetos (RF10–RF13, UC7, Figura 26).
    public class ProjectTests : ApiTestBase
    {
        public ProjectTests(ApiFactory factory) : base(factory) { }

        private async Task<(User Gerente, Portfolio Portfolio, HttpClient Client)> SetupAsync()
        {
            var gerente = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(gerente);
            return (gerente, portfolio, Factory.ClientFor(gerente));
        }

        private static object NovoProjeto(Guid responsavelId, string nome = "Projeto", string inicio = "2026-01-01", string fim = "2026-12-31", string categoria = "Grow") => new
        {
            nome,
            descricao = "desc",
            responsavelId,
            dataInicio = inicio,
            dataFim = fim,
            orcamentoAprovado = 100000,
            categoriaEstrategica = categoria,
            prioridade = "Alta",
            jiraProjectKey = "prj",
        };

        private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<JsonElement>();

        [Fact]
        public async Task Criar_DefineRascunhoENaoAvaliado()
        {
            var (gerente, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await JsonAsync(response);
            Assert.Equal("Rascunho", body.GetProperty("status").GetString());
            Assert.Equal("NaoAvaliado", body.GetProperty("statusAvaliacao").GetString());
            Assert.Equal("PRJ", body.GetProperty("jiraProjectKey").GetString());
            Assert.Equal("Grow", body.GetProperty("categoriaEstrategica").GetString());
            Assert.Equal(100000m, body.GetProperty("orcamentoAprovado").GetDecimal());
        }

        [Fact]
        public async Task CampoObrigatorioVazio_RN04()
        {
            var (_, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", new { nome = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await JsonAsync(response);
            Assert.Equal("Preencha os campos obrigatórios.", body.GetProperty("detail").GetString());
            Assert.True(body.GetProperty("errors").TryGetProperty("responsavelId", out _));
        }

        [Fact]
        public async Task DataFimAntesDoInicio_RN12()
        {
            var (gerente, portfolio, client) = await SetupAsync();

            var response = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos",
                NovoProjeto(gerente.Id, inicio: "2026-05-10", fim: "2026-05-09"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("A data de término deve ser igual ou posterior à data de início.", await DetailAsync(response));
        }

        [Fact]
        public async Task ProductOwnerNaoCriaProjeto_RN27()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var portfolio = await PortfolioAsync(po);

            var response = await Factory.ClientFor(po).PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(po.Id));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TodasAsTransicoesDaFigura26_EFinalizarPreencheDataConclusao()
        {
            var (gerente, portfolio, client) = await SetupAsync();
            var id = (await JsonAsync(await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id))))
                .GetProperty("id").GetGuid();

            async Task<string> Acao(string acao)
            {
                var r = await client.PostAsJsonAsync($"/api/projetos/{id}/status", new { acao });
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                return (await JsonAsync(r)).GetProperty("status").GetString()!;
            }

            Assert.Equal("Planejado", await Acao("Aprovar"));
            Assert.Equal("EmAndamento", await Acao("Iniciar"));
            Assert.Equal("EmRisco", await Acao("MarcarRisco"));
            Assert.Equal("EmAndamento", await Acao("MitigarRisco"));
            Assert.Equal("Suspenso", await Acao("Suspender"));
            Assert.Equal("EmAndamento", await Acao("Retomar"));
            Assert.Equal("Concluido", await Acao("Finalizar"));

            var detalhe = await JsonAsync(await client.GetAsync($"/api/projetos/{id}"));
            Assert.NotEqual(JsonValueKind.Null, detalhe.GetProperty("dataConclusao").ValueKind);

            Assert.Equal("Arquivado", await Acao("Arquivar"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("Aprovar")]
        [InlineData("Aprovar,Iniciar")]
        [InlineData("Aprovar,Iniciar,MarcarRisco")]
        [InlineData("Aprovar,Iniciar,Suspender")]
        public async Task Cancelar_PermitidoAntesDoFim(string acoes)
        {
            var caminho = acoes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var (gerente, portfolio, client) = await SetupAsync();
            var id = (await JsonAsync(await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id))))
                .GetProperty("id").GetGuid();
            foreach (var acao in caminho)
            {
                await client.PostAsJsonAsync($"/api/projetos/{id}/status", new { acao });
            }

            var response = await client.PostAsJsonAsync($"/api/projetos/{id}/status", new { acao = "Cancelar" });

            Assert.Equal("Cancelado", (await JsonAsync(response)).GetProperty("status").GetString());
        }

        [Fact]
        public async Task TransicaoInvalida_RN22_EProjetoCanceladoNaoEditavel()
        {
            var (gerente, portfolio, client) = await SetupAsync();
            var id = (await JsonAsync(await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id))))
                .GetProperty("id").GetGuid();

            var invalida = await client.PostAsJsonAsync($"/api/projetos/{id}/status", new { acao = "Finalizar" });
            Assert.Equal(HttpStatusCode.Conflict, invalida.StatusCode);
            Assert.Equal("Transição de 'Rascunho' para 'Finalizar' não permitida.", await DetailAsync(invalida));

            await client.PostAsJsonAsync($"/api/projetos/{id}/status", new { acao = "Cancelar" });
            var edit = await client.PutAsJsonAsync($"/api/projetos/{id}", NovoProjeto(gerente.Id, nome: "Novo nome"));
            Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        }

        [Fact]
        public async Task Listagem_FiltraPorStatusECategoria()
        {
            var (gerente, portfolio, client) = await SetupAsync();
            await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id, "Run 1", categoria: "Run"));
            await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/projetos", NovoProjeto(gerente.Id, "Transform 1", categoria: "Transform"));

            var lista = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/projetos?categoria=Transform&status=Rascunho");

            Assert.Single(lista.EnumerateArray());
            Assert.Equal("Transform 1", lista[0].GetProperty("nome").GetString());
        }
    }
}
