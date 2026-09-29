using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T04 — gestão de usuários (RF03).
    public class UsersTests : ApiTestBase
    {
        public UsersTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task Crud_FuncionaParaOAdministrador()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var client = Factory.ClientFor(admin);
            var email = $"novo{Guid.NewGuid():N}@Prumo.dev";

            var created = await client.PostAsJsonAsync("/api/usuarios", new { nome = "Novo", email, perfis = new[] { "QA", "TechLead" } });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var body = await created.Content.ReadFromJsonAsync<JsonElement>();
            var id = body.GetProperty("id").GetGuid();
            Assert.Equal(email.ToLowerInvariant(), body.GetProperty("email").GetString());

            var updated = await client.PutAsJsonAsync($"/api/usuarios/{id}", new { nome = "Novo Nome", perfis = new[] { "Diretoria" } });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var updatedBody = await updated.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Novo Nome", updatedBody.GetProperty("nome").GetString());
            Assert.Equal("Diretoria", updatedBody.GetProperty("perfis")[0].GetString());

            var list = await client.GetFromJsonAsync<JsonElement>("/api/usuarios");
            Assert.Contains(list.EnumerateArray(), u => u.GetProperty("id").GetGuid() == id);

            var deactivated = await client.PatchAsJsonAsync($"/api/usuarios/{id}/ativo", new { ativo = false });
            Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        }

        [Fact]
        public async Task EmailRepetido_Recebe409()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var existing = await UserAsync(RoleName.QA);

            var response = await Factory.ClientFor(admin).PostAsJsonAsync("/api/usuarios",
                new { nome = "Dup", email = existing.Email.ToUpperInvariant(), perfis = new[] { "QA" } });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Já existe um usuário com este e-mail.", await DetailAsync(response));
        }

        [Fact]
        public async Task SemPerfil_Recebe400()
        {
            var admin = await UserAsync(RoleName.Administrador);

            var response = await Factory.ClientFor(admin).PostAsJsonAsync("/api/usuarios",
                new { nome = "Sem perfil", email = "semperfil@prumo.dev", perfis = Array.Empty<string>() });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Selecione ao menos um perfil.", await DetailAsync(response));
        }

        [Fact]
        public async Task AdministradorNaoPodeDesativarASiMesmo()
        {
            var admin = await UserAsync(RoleName.Administrador);

            var response = await Factory.ClientFor(admin).PatchAsJsonAsync($"/api/usuarios/{admin.Id}/ativo", new { ativo = false });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Você não pode desativar o próprio usuário.", await DetailAsync(response));
        }

        [Fact]
        public async Task UsuarioDesativado_NaoConsegueLogar()
        {
            var admin = await UserAsync(RoleName.Administrador);
            var user = await UserAsync(RoleName.QA);

            await Factory.ClientFor(admin).PatchAsJsonAsync($"/api/usuarios/{user.Id}/ativo", new { ativo = false });
            var login = await Factory.CreateClient().PostAsJsonAsync("/api/auth/google", new { idToken = user.Email });

            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
            Assert.Equal("Usuário sem permissão de acesso ao Prumo.", await DetailAsync(login));
        }

        [Fact]
        public async Task UsuariosAtivos_DisponivelParaQualquerPerfil()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).GetAsync("/api/usuarios/ativos");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
