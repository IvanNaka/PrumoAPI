using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T09 — avaliação, score e ranking (RF18–RF21, UC10, Figura 28).
    public class PrioritizationTests : ApiTestBase
    {
        public PrioritizationTests(ApiFactory factory) : base(factory) { }

        private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<JsonElement>();

        private static async Task<Guid> CriarCriterioAsync(HttpClient client, Guid portfolioId, string nome, decimal peso, string tipo) =>
            (await JsonAsync(await client.PostAsJsonAsync($"/api/portfolios/{portfolioId}/criterios", new { nome, peso, tipo })))
            .GetProperty("id").GetGuid();

        private static async Task<Guid> CriarProjetoAsync(HttpClient client, Guid portfolioId, Guid responsavelId, string nome, string prioridade = "Media") =>
            (await JsonAsync(await client.PostAsJsonAsync($"/api/portfolios/{portfolioId}/projetos", new
            {
                nome, responsavelId, dataInicio = "2026-01-01", dataFim = "2026-12-31", orcamentoAprovado = 1000,
                categoriaEstrategica = "Run", prioridade,
            }))).GetProperty("id").GetGuid();

        private Task<string> PortfolioStatusAsync(Guid id) =>
            Factory.WithDbAsync(async db => (await db.Portfolios.AsNoTracking().SingleAsync(p => p.Id == id)).Status.ToString());

        [Fact]
        public async Task FluxoCompleto_CasoDoPlano_E_CicloDoPortfolio()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var client = Factory.ClientFor(admin);
            var portfolio = await PortfolioAsync(admin);

            var valor = await CriarCriterioAsync(client, portfolio.Id, "Valor", 4, "Beneficio");
            var esforco = await CriarCriterioAsync(client, portfolio.Id, "Esforço", 2, "Custo");
            var risco = await CriarCriterioAsync(client, portfolio.Id, "Risco", 2, "Custo");
            var alinhamento = await CriarCriterioAsync(client, portfolio.Id, "Alinhamento", 2, "Beneficio");
            Assert.Equal("Configurado", await PortfolioStatusAsync(portfolio.Id));

            var p1 = await CriarProjetoAsync(client, portfolio.Id, admin.Id, "Projeto A", "Media");
            var p2 = await CriarProjetoAsync(client, portfolio.Id, admin.Id, "Projeto B", "Alta");
            var p3 = await CriarProjetoAsync(client, portfolio.Id, admin.Id, "Projeto C");
            Assert.Equal("EmAnalise", await PortfolioStatusAsync(portfolio.Id));

            // Notas do caso de teste: Valor 5, Esforço 2, Risco 3, Alinhamento 4 -> 80,00
            object[] notas = { new { criterioId = valor, nota = 5 }, new { criterioId = esforco, nota = 2 }, new { criterioId = risco, nota = 3 }, new { criterioId = alinhamento, nota = 4 } };
            var avaliacao = await JsonAsync(await client.PutAsJsonAsync($"/api/projetos/{p1}/avaliacoes", notas));
            Assert.Equal("Avaliando", avaliacao.GetProperty("statusAvaliacao").GetString());
            await client.PutAsJsonAsync($"/api/projetos/{p2}/avaliacoes", notas);
            await client.PutAsJsonAsync($"/api/projetos/{p3}/avaliacoes", new[] { new { criterioId = valor, nota = 3 } });

            var resultado = await JsonAsync(await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null));
            var ranking = resultado.GetProperty("ranking");
            Assert.Equal(2, ranking.GetArrayLength());
            Assert.Equal(80.00m, ranking[0].GetProperty("score").GetDecimal());
            Assert.Equal(p2, ranking[0].GetProperty("projetoId").GetGuid()); // empate: Alta antes de Média
            Assert.Equal(1, ranking[0].GetProperty("posicao").GetInt32());
            var naoAvaliados = resultado.GetProperty("naoAvaliados");
            Assert.Equal(p3, naoAvaliados[0].GetProperty("projetoId").GetGuid());
            Assert.Equal(3, naoAvaliados[0].GetProperty("criteriosFaltantes").GetArrayLength());
            Assert.Equal("Priorizado", await PortfolioStatusAsync(portfolio.Id));

            // Aprovar portfólio -> Monitoramento -> Reavaliar -> Reavaliacao -> Priorizar -> Priorizado -> Aprovar -> Encerrar
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/aprovar", null)).StatusCode);
            Assert.Equal("Monitoramento", await PortfolioStatusAsync(portfolio.Id));
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/reavaliar", null)).StatusCode);
            Assert.Equal("Reavaliacao", await PortfolioStatusAsync(portfolio.Id));
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null)).StatusCode);
            Assert.Equal("Priorizado", await PortfolioStatusAsync(portfolio.Id));
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/aprovar", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/encerrar", null)).StatusCode);
            Assert.Equal("Encerrado", await PortfolioStatusAsync(portfolio.Id));

            // Qualquer escrita em portfólio encerrado -> RN23
            var escrita = await client.PutAsJsonAsync($"/api/projetos/{p1}/avaliacoes", notas);
            Assert.Equal(HttpStatusCode.Conflict, escrita.StatusCode);
            Assert.Equal("Portfólio encerrado não pode ser alterado.", await DetailAsync(escrita));
        }

        [Fact]
        public async Task AlterarPesoMudaORankingSemClicarEmNada_RF21()
        {
            var po = await UserAsync(RoleName.ProductOwner, RoleName.GerenteProjeto);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);
            var valor = await CriarCriterioAsync(client, portfolio.Id, "Valor", 9, "Beneficio");
            var esforco = await CriarCriterioAsync(client, portfolio.Id, "Esforço", 1, "Custo");
            var a = await CriarProjetoAsync(client, portfolio.Id, po.Id, "A (valor alto, esforço alto)");
            var b = await CriarProjetoAsync(client, portfolio.Id, po.Id, "B (valor médio, esforço baixo)");
            await client.PutAsJsonAsync($"/api/projetos/{a}/avaliacoes", new[] { new { criterioId = valor, nota = 5 }, new { criterioId = esforco, nota = 5 } });
            await client.PutAsJsonAsync($"/api/projetos/{b}/avaliacoes", new[] { new { criterioId = valor, nota = 3 }, new { criterioId = esforco, nota = 1 } });
            var antes = await JsonAsync(await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null));
            Assert.Equal(a, antes.GetProperty("ranking")[0].GetProperty("projetoId").GetGuid());

            // Esforço passa a pesar muito mais: B assume a liderança automaticamente.
            var pesos = await client.PutAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios/pesos",
                new[] { new { criterioId = valor, peso = 1m }, new { criterioId = esforco, peso = 9m } });
            Assert.Equal(HttpStatusCode.OK, pesos.StatusCode);

            var depois = await client.GetFromJsonAsync<JsonElement>($"/api/portfolios/{portfolio.Id}/ranking");
            Assert.Equal(b, depois.GetProperty("ranking")[0].GetProperty("projetoId").GetGuid());
        }

        [Fact]
        public async Task SemCriterios_RN15_ESemProjetos_RN16()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);

            var semCriterios = await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null);
            Assert.Equal(HttpStatusCode.Conflict, semCriterios.StatusCode);
            Assert.Equal("Cadastre ao menos um critério antes de priorizar.", await DetailAsync(semCriterios));

            await CriarCriterioAsync(client, portfolio.Id, "Valor", 3, "Beneficio");
            var semProjetos = await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null);
            Assert.Equal(HttpStatusCode.Conflict, semProjetos.StatusCode);
            Assert.Equal("Não há projetos cadastrados para priorização.", await DetailAsync(semProjetos));
        }

        [Fact]
        public async Task PriorizarExigeSomaDosPesosIgualA10()
        {
            var po = await UserAsync(RoleName.ProductOwner, RoleName.GerenteProjeto);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);
            await CriarCriterioAsync(client, portfolio.Id, "Valor", 6, "Beneficio");
            await CriarProjetoAsync(client, portfolio.Id, po.Id, "P");

            var response = await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("A soma dos pesos dos critérios deve ser exatamente 10.", await DetailAsync(response));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task NotaForaDe1a5_RN17(int nota)
        {
            var po = await UserAsync(RoleName.ProductOwner, RoleName.GerenteProjeto);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);
            var valor = await CriarCriterioAsync(client, portfolio.Id, "Valor", 3, "Beneficio");
            var projeto = await CriarProjetoAsync(client, portfolio.Id, po.Id, "P");

            var response = await client.PutAsJsonAsync($"/api/projetos/{projeto}/avaliacoes", new[] { new { criterioId = valor, nota } });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("A nota deve estar entre 1 e 5.", await DetailAsync(response));
        }

        [Fact]
        public async Task AprovarSoPriorizado_RN29_EAprovarLevaRascunhoParaPlanejado()
        {
            var po = await UserAsync(RoleName.ProductOwner, RoleName.GerenteProjeto);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);
            var valor = await CriarCriterioAsync(client, portfolio.Id, "Valor", 3, "Beneficio");
            var projeto = await CriarProjetoAsync(client, portfolio.Id, po.Id, "P");

            var cedo = await client.PostAsync($"/api/projetos/{projeto}/avaliacao/aprovar", null);
            Assert.Equal(HttpStatusCode.Conflict, cedo.StatusCode);
            Assert.Equal("Apenas projetos priorizados podem ser aprovados ou rejeitados.", await DetailAsync(cedo));

            await client.PutAsJsonAsync($"/api/projetos/{projeto}/avaliacoes", new[] { new { criterioId = valor, nota = 4 } });
            await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null);
            var aprovado = await JsonAsync(await client.PostAsync($"/api/projetos/{projeto}/avaliacao/aprovar", null));
            Assert.Equal("Aprovado", aprovado.GetProperty("statusAvaliacao").GetString());

            var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projeto}");
            Assert.Equal("Planejado", detalhe.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CriterioNovo_Reavaliado_VoltaAPriorizadoQuandoNotasCompletas()
        {
            var po = await UserAsync(RoleName.ProductOwner, RoleName.GerenteProjeto);
            var client = Factory.ClientFor(po);
            var portfolio = await PortfolioAsync(po);
            var valor = await CriarCriterioAsync(client, portfolio.Id, "Valor", 3, "Beneficio");
            var projeto = await CriarProjetoAsync(client, portfolio.Id, po.Id, "P");
            await client.PutAsJsonAsync($"/api/projetos/{projeto}/avaliacoes", new[] { new { criterioId = valor, nota = 4 } });
            await client.PostAsync($"/api/portfolios/{portfolio.Id}/priorizacao", null);

            var risco = await CriarCriterioAsync(client, portfolio.Id, "Risco", 2, "Custo");
            var reavaliado = await client.GetFromJsonAsync<JsonElement>($"/api/projetos/{projeto}");
            Assert.Equal("Reavaliado", reavaliado.GetProperty("statusAvaliacao").GetString());

            var completa = await JsonAsync(await client.PutAsJsonAsync($"/api/projetos/{projeto}/avaliacoes", new[] { new { criterioId = risco, nota = 2 } }));
            Assert.Equal("Priorizado", completa.GetProperty("statusAvaliacao").GetString());
            Assert.Equal(1, completa.GetProperty("posicaoRanking").GetInt32());
        }
    }
}
