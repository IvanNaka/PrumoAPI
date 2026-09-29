using System.Net;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T03 — perfis, policies e acesso ao portfólio (RF02, D02, D11).
    public class AccessControlTests : ApiTestBase
    {
        public AccessControlTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task PerfilSemPermissao_Recebe403ComRN27()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).GetAsync("/api/usuarios");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Seu perfil não tem permissão para esta ação.", await DetailAsync(response));
        }

        [Fact]
        public async Task Administrador_TemAcessoATudo()
        {
            var admin = await UserAsync(RoleName.Administrador);

            var response = await Factory.ClientFor(admin).GetAsync("/api/usuarios");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SemToken_Recebe401()
        {
            var response = await Factory.CreateClient().GetAsync("/api/usuarios");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task NaoMembro_Recebe403ComRN06()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var outsider = await UserAsync(RoleName.GerenteProjeto);
            var portfolio = await PortfolioAsync(owner);

            var response = await Factory.ClientFor(outsider).GetAsync($"/api/portfolios/{portfolio.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Você não tem permissão para acessar este portfólio.", await DetailAsync(response));
        }

        [Theory]
        [InlineData(RoleName.Diretoria)]
        [InlineData(RoleName.Administrador)]
        public async Task DiretoriaEAdministrador_VeemTodosOsPortfolios(RoleName role)
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var viewer = await UserAsync(role);
            var portfolio = await PortfolioAsync(owner);

            var response = await Factory.ClientFor(viewer).GetAsync($"/api/portfolios/{portfolio.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Membro_AcessaOPortfolio()
        {
            var owner = await UserAsync(RoleName.ProductOwner);
            var member = await UserAsync(RoleName.QA);
            var portfolio = await PortfolioAsync(owner, PortfolioStatus.Criado, member);

            var response = await Factory.ClientFor(member).GetAsync($"/api/portfolios/{portfolio.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
