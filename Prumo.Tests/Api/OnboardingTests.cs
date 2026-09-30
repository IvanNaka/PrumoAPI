using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // Primeiro acesso: usuário sem perfil entra em uma equipe pelo código de convite ou cria uma.
    public class OnboardingTests : ApiTestBase
    {
        public OnboardingTests(ApiFactory factory) : base(factory) { }

        private static string NomeEquipe() => "Squad " + Guid.NewGuid().ToString("N")[..6];

        private static List<string> Roles(JsonElement session) =>
            new JwtSecurityTokenHandler().ReadJwtToken(session.GetProperty("token").GetString())
                .Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();

        private HttpClient ClientWithToken(JsonElement session)
        {
            var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("token").GetString());
            return client;
        }

        [Fact]
        public async Task CriarEquipe_UsuarioViraAdministradorEMembro()
        {
            var pendente = await UserAsync();
            var nome = NomeEquipe();

            var response = await Factory.ClientFor(pendente).PostAsJsonAsync("/api/onboarding/equipes", new { nome });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var session = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "Administrador" }, Roles(session));

            var client = ClientWithToken(session);
            var equipes = await client.GetFromJsonAsync<JsonElement>("/api/equipes");
            var equipe = equipes.EnumerateArray().Single(e => e.GetProperty("nome").GetString() == nome);
            Assert.Equal(pendente.Id, equipe.GetProperty("membros")[0].GetProperty("usuarioId").GetGuid());
            Assert.Equal(8, equipe.GetProperty("codigoConvite").GetString()!.Length);
        }

        [Fact]
        public async Task EntrarComCodigo_UsuarioViraDesenvolvedor()
        {
            var tl = await UserAsync(RoleName.TechLead);
            var equipe = await (await Factory.ClientFor(tl).PostAsJsonAsync("/api/equipes", new { nome = NomeEquipe() }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var codigo = equipe.GetProperty("codigoConvite").GetString()!;
            var pendente = await UserAsync();

            var response = await Factory.ClientFor(pendente).PostAsJsonAsync("/api/onboarding/entrar", new { codigo = " " + codigo.ToLower() + " " });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var session = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "Desenvolvedor" }, Roles(session));

            var client = ClientWithToken(session);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/portfolios")).StatusCode);
            var membros = await client.GetFromJsonAsync<JsonElement>($"/api/equipes/{equipe.GetProperty("id").GetGuid()}/membros");
            Assert.Contains(membros.EnumerateArray(), m => m.GetProperty("usuarioId").GetGuid() == pendente.Id);

            // Desenvolvedor não vê o código de convite.
            var vista = await client.GetFromJsonAsync<JsonElement>($"/api/equipes/{equipe.GetProperty("id").GetGuid()}");
            Assert.Equal(JsonValueKind.Null, vista.GetProperty("codigoConvite").ValueKind);
        }

        [Fact]
        public async Task CodigoInvalido_Recebe404()
        {
            var pendente = await UserAsync();

            var response = await Factory.ClientFor(pendente).PostAsJsonAsync("/api/onboarding/entrar", new { codigo = "NAOEXISTE" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Código de convite inválido.", await DetailAsync(response));
        }

        [Fact]
        public async Task UsuarioComPerfil_NaoPassaPeloOnboarding()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).PostAsJsonAsync("/api/onboarding/equipes", new { nome = NomeEquipe() });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Você já participa do Prumo.", await DetailAsync(response));
        }

        [Fact]
        public async Task NovoCodigo_InvalidaOAnterior()
        {
            var tl = await UserAsync(RoleName.TechLead);
            var client = Factory.ClientFor(tl);
            var equipe = await (await client.PostAsJsonAsync("/api/equipes", new { nome = NomeEquipe() }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var antigo = equipe.GetProperty("codigoConvite").GetString();

            var novo = await (await client.PostAsync($"/api/equipes/{equipe.GetProperty("id").GetGuid()}/codigo-convite", null))
                .Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(antigo, novo.GetProperty("codigoConvite").GetString());

            var pendente = await UserAsync();
            var response = await Factory.ClientFor(pendente).PostAsJsonAsync("/api/onboarding/entrar", new { codigo = antigo });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
