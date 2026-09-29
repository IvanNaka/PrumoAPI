using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T05 — cadastro, listagem e ciclo de vida do portfólio (RF04, RF05, UC2, Figura 27).
    public class PortfolioTests : ApiTestBase
    {
        public PortfolioTests(ApiFactory factory) : base(factory) { }

        private async Task<string> StatusAsync(Guid id) =>
            await Factory.WithDbAsync(async db => (await db.Portfolios.AsNoTracking().SingleAsync(p => p.Id == id)).Status.ToString());

        private Task SetStatusAsync(Guid id, PortfolioStatus status) =>
            Factory.WithDbAsync(async db =>
            {
                var portfolio = await db.Portfolios.SingleAsync(p => p.Id == id);
                portfolio.Status = status;
                await db.SaveChangesAsync();
            });

        [Fact]
        public async Task Criar_DefineStatusCriado_EIncluiResponsavelComoMembro()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var gerente = await UserAsync(RoleName.GerenteProjeto);

            var response = await Factory.ClientFor(po).PostAsJsonAsync("/api/portfolios",
                new { nome = "Transformação Digital " + Guid.NewGuid(), descricao = "d", objetivo = "Crescer 20%", responsavelId = gerente.Id });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Criado", body.GetProperty("status").GetString());
            Assert.Equal("Crescer 20%", body.GetProperty("objetivo").GetString());
            var id = body.GetProperty("id").GetGuid();

            // o responsável vê o portfólio (é membro)
            var list = await Factory.ClientFor(gerente).GetFromJsonAsync<JsonElement>("/api/portfolios");
            Assert.Contains(list.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id);
        }

        [Fact]
        public async Task Criar_SemNome_Recebe400ComRN04()
        {
            var po = await UserAsync(RoleName.ProductOwner);

            var response = await Factory.ClientFor(po).PostAsJsonAsync("/api/portfolios", new { nome = "", responsavelId = po.Id });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Preencha os campos obrigatórios.", await DetailAsync(response));
        }

        [Fact]
        public async Task Criar_ComPerfilSemPermissao_Recebe403()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).PostAsJsonAsync("/api/portfolios", new { nome = "X", responsavelId = dev.Id });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Listagem_AplicaD11()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var outsider = await UserAsync(RoleName.QA);
            var diretoria = await UserAsync(RoleName.Diretoria);
            var portfolio = await PortfolioAsync(owner);

            var outsiderList = await Factory.ClientFor(outsider).GetFromJsonAsync<JsonElement>("/api/portfolios");
            var diretoriaList = await Factory.ClientFor(diretoria).GetFromJsonAsync<JsonElement>("/api/portfolios");

            Assert.DoesNotContain(outsiderList.EnumerateArray(), p => p.GetProperty("id").GetGuid() == portfolio.Id);
            Assert.Contains(diretoriaList.EnumerateArray(), p => p.GetProperty("id").GetGuid() == portfolio.Id);
        }

        [Fact]
        public async Task CicloDeVida_TransicoesDaFigura27()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var client = Factory.ClientFor(admin);
            var portfolio = await PortfolioAsync(admin);

            // Criado -> Configurado (primeiro critério, automático)
            var criterio = await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Valor", peso = 4, tipo = "Beneficio" });
            Assert.True(criterio.IsSuccessStatusCode);
            Assert.Equal("Configurado", await StatusAsync(portfolio.Id));

            // Configurado -> EmAnalise (primeiro projeto, automático)
            var projeto = await client.PostAsJsonAsync("/api/Projects", new { name = "P1", portfolioId = portfolio.Id });
            Assert.True(projeto.IsSuccessStatusCode);
            Assert.Equal("EmAnalise", await StatusAsync(portfolio.Id));

            // Aprovar só a partir de Priorizado (RN22)
            var aprovarCedo = await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/aprovar", null);
            Assert.Equal(HttpStatusCode.Conflict, aprovarCedo.StatusCode);
            Assert.Equal("Transição de 'EmAnalise' para 'Aprovar' não permitida.", await DetailAsync(aprovarCedo));

            // (a priorização é coberta em PrioritizationTests; aqui o status é ajustado direto)
            await SetStatusAsync(portfolio.Id, PortfolioStatus.Priorizado);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/aprovar", null)).StatusCode);
            Assert.Equal("Monitoramento", await StatusAsync(portfolio.Id));

            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/reavaliar", null)).StatusCode);
            Assert.Equal("Reavaliacao", await StatusAsync(portfolio.Id));

            await SetStatusAsync(portfolio.Id, PortfolioStatus.Monitoramento);
            // Monitoramento + critério alterado -> Reavaliacao (automático)
            await client.PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/criterios", new { nome = "Risco", peso = 2, tipo = "Custo" });
            Assert.Equal("Reavaliacao", await StatusAsync(portfolio.Id));

            await SetStatusAsync(portfolio.Id, PortfolioStatus.Monitoramento);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/portfolios/{portfolio.Id}/acoes/encerrar", null)).StatusCode);
            Assert.Equal("Encerrado", await StatusAsync(portfolio.Id));
        }

        [Fact]
        public async Task PortfolioEncerrado_RecusaEscritaComRN23()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var portfolio = await PortfolioAsync(admin, PortfolioStatus.Encerrado);

            var response = await Factory.ClientFor(admin).PutAsJsonAsync($"/api/portfolios/{portfolio.Id}",
                new { nome = "Novo nome", responsavelId = admin.Id });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Portfólio encerrado não pode ser alterado.", await DetailAsync(response));
        }

        [Fact]
        public async Task Membros_SomenteResponsavelOuAdminGerencia()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var member = await UserAsync(RoleName.GerenteProjeto);
            var novo = await UserAsync(RoleName.QA);
            var portfolio = await PortfolioAsync(owner, PortfolioStatus.Criado, member);

            var byMember = await Factory.ClientFor(member).PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/membros", new { usuarioId = novo.Id });
            Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);

            var byOwner = await Factory.ClientFor(owner).PostAsJsonAsync($"/api/portfolios/{portfolio.Id}/membros", new { usuarioId = novo.Id });
            Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);

            var removeOwner = await Factory.ClientFor(owner).DeleteAsync($"/api/portfolios/{portfolio.Id}/membros/{owner.Id}");
            Assert.Equal(HttpStatusCode.Conflict, removeOwner.StatusCode);

            var remove = await Factory.ClientFor(owner).DeleteAsync($"/api/portfolios/{portfolio.Id}/membros/{novo.Id}");
            Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
        }
    }
}
