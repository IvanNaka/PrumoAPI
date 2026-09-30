using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T02 — critérios de aceite do login Google (UC1, RN01–RN03).
    public class AuthTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public AuthTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static async Task<string?> DetailAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("detail").GetString();
        }

        [Fact]
        public async Task EmailCadastradoEAtivo_Entra_EJwtTemUmaRolePorPerfil()
        {
            await _factory.CreateUserAsync("ativo@prumo.dev", true, RoleName.ProductOwner, RoleName.TechLead);
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "Ativo@Prumo.dev" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = body.GetProperty("token").GetString()!;
            Assert.True(body.TryGetProperty("expiraEm", out _));
            Assert.Equal("ativo@prumo.dev", body.GetProperty("usuario").GetProperty("email").GetString());

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).OrderBy(v => v).ToList();
            Assert.Equal(new[] { "ProductOwner", "TechLead" }, roles);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var me = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        }

        [Fact]
        public async Task EmailNaoCadastrado_EntraSemPerfil_ESoAcessaOOnboarding()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/auth/google", new { idToken = "Novo@Prumo.dev" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var usuario = body.GetProperty("usuario");
            Assert.Equal("novo@prumo.dev", usuario.GetProperty("email").GetString());
            Assert.Equal("Nome Novo", usuario.GetProperty("nome").GetString());
            Assert.Empty(usuario.GetProperty("perfis").EnumerateArray());

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

            var portfolios = await client.GetAsync("/api/portfolios");
            Assert.Equal(HttpStatusCode.Forbidden, portfolios.StatusCode);
            Assert.Equal("Entre em uma equipe ou crie uma para acessar o Prumo.", await DetailAsync(portfolios));

            // O segundo login reaproveita o usuário criado no primeiro.
            var again = await _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = "novo@prumo.dev" });
            var againBody = await again.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(usuario.GetProperty("id").GetGuid(), againBody.GetProperty("usuario").GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task EmailNaoCadastradoJaMembroDeEquipe_EntraComoDesenvolvedor()
        {
            var team = new Prumo.Domain.Entities.Team { Name = "Equipe " + Guid.NewGuid().ToString("N")[..6] };
            team.Members.Add(new Prumo.Domain.Entities.TeamUser
            {
                TeamId = team.Id, Name = "Membro", Email = "membro-previo@prumo.dev", HourlyCost = 10, MonthlyCapacityHours = 100,
            });
            await _factory.WithDbAsync(async db => { db.Teams.Add(team); await db.SaveChangesAsync(); });

            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = "membro-previo@prumo.dev" });

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "Desenvolvedor" }, body.GetProperty("usuario").GetProperty("perfis").EnumerateArray().Select(p => p.GetString()));
            var userId = body.GetProperty("usuario").GetProperty("id").GetGuid();
            var linked = await _factory.WithDbAsync(db => Task.FromResult(db.TeamUsers.Single(m => m.TeamId == team.Id).UserId));
            Assert.Equal(userId, linked);
        }

        [Fact]
        public async Task UsuarioInativo_Recebe403()
        {
            await _factory.CreateUserAsync("inativo@prumo.dev", false, RoleName.Desenvolvedor);

            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = "inativo@prumo.dev" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("Usuário sem permissão de acesso ao Prumo.", await DetailAsync(response));
        }

        [Fact]
        public async Task TokenGoogleInvalido_Recebe401ComRN01()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = "invalido" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Token de autenticação inválido.", await DetailAsync(response));
        }

        [Fact]
        public async Task FalhaNoGoogle_Recebe503ComRN02()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = "offline" });

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("Não foi possível realizar o login. Tente novamente.", await DetailAsync(response));
        }

        [Fact]
        public async Task TokenAdulterado_Recebe401()
        {
            var user = await _factory.CreateUserAsync("adulterado@prumo.dev", true, RoleName.Desenvolvedor);
            var token = _factory.TokenFor(user);
            var tampered = token[..^4] + (token.EndsWith("AAAA") ? "BBBB" : "AAAA");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
            var response = await client.GetAsync("/api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
