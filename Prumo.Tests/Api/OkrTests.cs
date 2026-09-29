using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    // T10 — OKRs e Key Results (RF14, RF15, RF17, UC8).
    public class OkrTests : ApiTestBase
    {
        public OkrTests(ApiFactory factory) : base(factory) { }

        [Fact]
        public async Task CriarOkr_ComProgressoF4_EAtualizarValorDoKr()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var client = Factory.ClientFor(po);

            var response = await client.PostAsJsonAsync("/api/okrs", new
            {
                titulo = "Aumentar receita digital",
                descricao = "OKR do ano",
                dataInicio = "2026-01-01",
                dataFim = "2026-12-31",
                keyResults = new object[]
                {
                    new { descricao = "Receita (R$ mil)", meta = 100, valorAtual = 50 },
                    new { descricao = "Novos clientes", meta = 10, valorAtual = 12 },
                },
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var okr = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(75m, okr.GetProperty("progresso").GetDecimal());
            var krId = okr.GetProperty("keyResults")[0].GetProperty("id").GetGuid();

            var kr = await (await client.PutAsJsonAsync($"/api/key-results/{krId}", new { valorAtual = 100 }))
                .Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(100m, kr.GetProperty("progresso").GetDecimal());

            var lista = await client.GetFromJsonAsync<JsonElement>("/api/okrs");
            var atualizado = lista.EnumerateArray().Single(o => o.GetProperty("id").GetGuid() == okr.GetProperty("id").GetGuid());
            Assert.Equal(100m, atualizado.GetProperty("progresso").GetDecimal());
        }

        [Fact]
        public async Task OkrSemKeyResult_RN13()
        {
            var po = await UserAsync(RoleName.ProductOwner);

            var response = await Factory.ClientFor(po).PostAsJsonAsync("/api/okrs", new { titulo = "Sem KR", keyResults = Array.Empty<object>() });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Um objetivo precisa de pelo menos um Key Result.", await DetailAsync(response));
        }

        [Fact]
        public async Task EditarOkr_SemKr_RN13()
        {
            var po = await UserAsync(RoleName.ProductOwner);
            var client = Factory.ClientFor(po);
            var okr = await (await client.PostAsJsonAsync("/api/okrs", new
            {
                titulo = "OKR", keyResults = new[] { new { descricao = "KR", meta = 1, valorAtual = 0 } },
            })).Content.ReadFromJsonAsync<JsonElement>();

            var response = await client.PutAsJsonAsync($"/api/okrs/{okr.GetProperty("id").GetGuid()}", new { titulo = "OKR", keyResults = Array.Empty<object>() });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Um objetivo precisa de pelo menos um Key Result.", await DetailAsync(response));
        }

        [Fact]
        public async Task SomenteProductOwnerEditaOkrs()
        {
            var dev = await UserAsync(RoleName.Desenvolvedor);

            var response = await Factory.ClientFor(dev).PostAsJsonAsync("/api/okrs", new { titulo = "X", keyResults = new[] { new { descricao = "KR", meta = 1 } } });
            var lista = await Factory.ClientFor(dev).GetAsync("/api/okrs");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        }
    }
}
