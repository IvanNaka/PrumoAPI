using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T16 — configurar e testar a conexão com o Jira (RF47, UC15, Figura 29).
    [Collection("Jira")]
    public class JiraConfigTests : ApiTestBase
    {
        public JiraConfigTests(ApiFactory factory) : base(factory) { }

        private static object Config(string token) => new
        {
            url = "https://empresa.atlassian.net/",
            email = "Jira@Empresa.com",
            apiToken = token,
            intervaloSincronizacaoMinutos = 30,
            ativo = true,
        };

        [Fact]
        public async Task CredenciaisErradas_ErroConexao_RN24_EConfiguracaoFicaSalva()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);
            var client = Factory.ClientFor(dev);

            var response = await client.PutAsJsonAsync("/api/integracoes/jira", Config("token-errado"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Falha de autenticação no Jira. Verifique URL, e-mail e token.", await DetailAsync(response));
            var atual = await client.GetFromJsonAsync<JsonElement>("/api/integracoes/jira");
            Assert.Equal("ErroConexao", atual.GetProperty("status").GetString());
            Assert.Equal("https://empresa.atlassian.net", atual.GetProperty("url").GetString());
        }

        [Fact]
        public async Task CredenciaisCorretas_Conectada_ETokenNuncaApareceNaApi()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);
            var client = Factory.ClientFor(dev);

            var response = await client.PutAsJsonAsync("/api/integracoes/jira", Config(ApiFactory.FakeJiraProvider.TokenValido));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"status\":\"Conectada\"", body);
            Assert.DoesNotContain(ApiFactory.FakeJiraProvider.TokenValido, body);

            var get = await client.GetStringAsync("/api/integracoes/jira");
            Assert.DoesNotContain(ApiFactory.FakeJiraProvider.TokenValido, get);
            Assert.Contains("\"tokenConfigurado\":true", get);

            // O token fica criptografado no banco.
            var gravado = await Factory.WithDbAsync(db => db.Integrations.AsNoTracking().SingleAsync(i => i.Type == IntegrationType.Jira));
            Assert.NotEqual(ApiFactory.FakeJiraProvider.TokenValido, gravado.Token);

            // Editar sem token mantém o token atual.
            var semToken = await client.PutAsJsonAsync("/api/integracoes/jira", Config(""));
            Assert.Equal(HttpStatusCode.OK, semToken.StatusCode);
            var fake = Factory.Services.GetRequiredService<ApiFactory.FakeJiraProvider>();
            Assert.Equal(ApiFactory.FakeJiraProvider.TokenValido, fake.Testes[^1].ApiToken);
            Assert.Equal("jira@empresa.com", fake.Testes[^1].Email);
        }

        [Fact]
        public async Task IntervaloForaDe15a1440_Recebe400_EQaNaoConfigura()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);
            var qa = await UserAsync(RoleName.QA);

            var intervalo = await Factory.ClientFor(dev).PutAsJsonAsync("/api/integracoes/jira", new
            {
                url = "https://x.atlassian.net", email = "a@b.com", apiToken = "t", intervaloSincronizacaoMinutos = 5, ativo = true,
            });
            var porQa = await Factory.ClientFor(qa).GetAsync("/api/integracoes/jira");

            Assert.Equal(HttpStatusCode.BadRequest, intervalo.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, porQa.StatusCode);
        }
    }
}
