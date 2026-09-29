using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T15 — equipes, membros e capacidade (RF29–RF32, UC13).
    public class TeamTests : ApiTestBase
    {
        public TeamTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task EquipeEMembros_ECapacidade()
        {
            var tl = await UserAsync(RoleName.TechLead);
            var client = Factory.ClientFor(tl);

            var equipe = await (await client.PostAsJsonAsync("/api/equipes", new { nome = "Squad " + Guid.NewGuid().ToString("N")[..6] }))
                .Content.ReadFromJsonAsync<JsonElement>();
            var id = equipe.GetProperty("id").GetGuid();

            var semMembros = await client.GetFromJsonAsync<JsonElement>($"/api/equipes/{id}/capacidade?mes=2026-06");
            Assert.False(semMembros.GetProperty("disponivel").GetBoolean());
            Assert.Equal("Dados insuficientes para calcular este indicador.", semMembros.GetProperty("motivo").GetString());

            var membro = await client.PostAsJsonAsync($"/api/equipes/{id}/membros",
                new { nome = "Ana", email = "Ana@Prumo.dev", custoHora = 120, capacidadeMensalHoras = 160 });
            Assert.Equal(HttpStatusCode.Created, membro.StatusCode);

            var capacidade = await client.GetFromJsonAsync<JsonElement>($"/api/equipes/{id}/capacidade?mes=2026-06");
            Assert.True(capacidade.GetProperty("disponivel").GetBoolean());
            Assert.Equal(160m, capacidade.GetProperty("capacidade").GetDecimal());
            Assert.Single(capacidade.GetProperty("membros").EnumerateArray());
        }

        [Fact]
        public async Task CapacidadeForaDoIntervalo_Recebe400()
        {
            var tl = await UserAsync(RoleName.TechLead);
            var client = Factory.ClientFor(tl);
            var equipe = await (await client.PostAsJsonAsync("/api/equipes", new { nome = "Squad " + Guid.NewGuid().ToString("N")[..6] }))
                .Content.ReadFromJsonAsync<JsonElement>();

            var response = await client.PostAsJsonAsync($"/api/equipes/{equipe.GetProperty("id").GetGuid()}/membros",
                new { nome = "X", email = "x@prumo.dev", custoHora = 10, capacidadeMensalHoras = 301 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task NomeRepetido_Recebe409_EPoNaoEditaEquipes()
        {
            var tl = await UserAsync(RoleName.TechLead);
            var po = await UserAsync(RoleName.ProductOwner);
            var nome = "Squad " + Guid.NewGuid().ToString("N")[..6];
            await Factory.ClientFor(tl).PostAsJsonAsync("/api/equipes", new { nome });

            var repetido = await Factory.ClientFor(tl).PostAsJsonAsync("/api/equipes", new { nome = nome.ToUpperInvariant() });
            var porPo = await Factory.ClientFor(po).PostAsJsonAsync("/api/equipes", new { nome = "Outra" });
            var leitura = await Factory.ClientFor(po).GetAsync("/api/equipes");

            Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, porPo.StatusCode);
            Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
        }
    }
}
