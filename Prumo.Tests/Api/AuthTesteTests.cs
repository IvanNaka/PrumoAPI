using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // Seção 4.9 b — login de teste das contas simuladas, só no ambiente de testes.
    public class AuthTesteTests : IClassFixture<ApiFactory>
    {
        private const string Chave = "chave-do-login-de-teste-123";
        private readonly ApiFactory _factory;

        public AuthTesteTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private HttpClient Cliente(string habilitado = "true", string chave = Chave, string? ambiente = null)
        {
            return _factory.WithWebHostBuilder(b =>
            {
                b.UseSetting("AuthTeste:Habilitado", habilitado);
                b.UseSetting("AuthTeste:Chave", chave);
                if (ambiente != null)
                {
                    b.UseEnvironment(ambiente);
                }
            }).CreateClient();
        }

        private static Task<HttpResponseMessage> EntrarAsync(HttpClient client, string email, string? chave = Chave)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/teste") { Content = JsonContent.Create(new { email }) };
            if (chave != null)
            {
                request.Headers.Add("X-Auth-Teste-Chave", chave);
            }
            return client.SendAsync(request);
        }

        [Fact]
        public async Task Habilitado_EmiteJwtDoUsuarioCadastrado()
        {
            await _factory.CreateUserAsync("po.teste@prumo.dev", true, RoleName.ProductOwner);
            var client = Cliente();

            var response = await EntrarAsync(client, "PO.Teste@prumo.dev");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("ProductOwner", body.GetProperty("usuario").GetProperty("perfis")[0].GetString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        }

        [Fact]
        public async Task EmailNaoCadastradoOuInativo_Retorna403_ENaoCadastra()
        {
            await _factory.CreateUserAsync("inativo.teste@prumo.dev", false, RoleName.QA);
            var client = Cliente();

            Assert.Equal(HttpStatusCode.Forbidden, (await EntrarAsync(client, "ninguem@prumo.dev")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await EntrarAsync(client, "inativo.teste@prumo.dev")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await EntrarAsync(client, "ninguem@prumo.dev")).StatusCode);
        }

        [Theory]
        [InlineData("false", Chave, null)]          // flag desligada
        [InlineData("true", "curta", null)]         // chave com menos de 16 caracteres
        [InlineData("true", Chave, "Production")]   // nunca em produção
        public async Task ForaDoAmbienteDeTestes_Retorna404(string habilitado, string chave, string? ambiente)
        {
            await _factory.CreateUserAsync($"{Guid.NewGuid():N}@prumo.dev", true, RoleName.Administrador);
            var client = Cliente(habilitado, chave, ambiente);

            var response = await EntrarAsync(client, "qualquer@prumo.dev", chave);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChaveAusenteOuErrada_Retorna404()
        {
            await _factory.CreateUserAsync("dev.teste@prumo.dev", true, RoleName.Desenvolvedor);
            var client = Cliente();

            Assert.Equal(HttpStatusCode.NotFound, (await EntrarAsync(client, "dev.teste@prumo.dev", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await EntrarAsync(client, "dev.teste@prumo.dev", "outra-chave-qualquer-123")).StatusCode);
        }
    }
}
