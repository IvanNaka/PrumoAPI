using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Prumo.Application.DTOs.Integration;
using Prumo.Infrastructure.Integrations;

namespace Prumo.Tests.Integrations
{
    /// <summary>Responde a partir de uma função; guarda as requisições recebidas.</summary>
    public class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string?> Bodies { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responder(request);
        }

        public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    // T16 — teste de conexão: GET {url}/rest/api/3/myself com Basic base64(email:apiToken).
    public class JiraProviderTests
    {
        private static readonly IntegrationCredentials Credenciais = new("https://empresa.atlassian.net/", "ana@empresa.com", "segredo");

        [Fact]
        public async Task TestarConexao_ChamaMyselfComBasicAuth()
        {
            var handler = new StubHandler(_ => StubHandler.Json("{\"accountId\":\"1\"}"));
            var provider = new JiraIntegrationProvider(new HttpClient(handler), NullLogger<JiraIntegrationProvider>.Instance);

            var ok = await provider.TestConnectionAsync(Credenciais);

            Assert.True(ok);
            var request = handler.Requests.Single();
            Assert.Equal("https://empresa.atlassian.net/rest/api/3/myself", request.RequestUri!.ToString());
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            Assert.Equal("ana@empresa.com:segredo", Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task TestarConexao_QualquerOutraRespostaEhFalha(HttpStatusCode status)
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(status));
            var provider = new JiraIntegrationProvider(new HttpClient(handler), NullLogger<JiraIntegrationProvider>.Instance);

            Assert.False(await provider.TestConnectionAsync(Credenciais));
        }
    }
}
