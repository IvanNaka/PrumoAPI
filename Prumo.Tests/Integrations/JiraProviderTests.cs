using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Prumo.Application.DTOs.Integration;
using Prumo.Domain.Enums;
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
            private static JiraIntegrationProvider Provider(StubHandler handler) =>
            new(new HttpClient(handler), NullLogger<JiraIntegrationProvider>.Instance);

        private static string IssueJson(string key, string type, string category = "indeterminate", bool subtask = false)
        {
            var resolution = category == "done" ? "\"2026-09-05T12:00:00.000+0000\"" : "null";
            return "{\"key\":\"" + key + "\",\"fields\":{\"summary\":\"Resumo " + key + "\","
                + "\"issuetype\":{\"name\":\"" + type + "\",\"subtask\":" + (subtask ? "true" : "false") + "},"
                + "\"status\":{\"name\":\"Status\",\"statusCategory\":{\"key\":\"" + category + "\"}},"
                + "\"created\":\"2026-09-01T10:00:00.000-0300\",\"updated\":\"2026-09-02T10:00:00.000+0000\","
                + "\"resolutiondate\":" + resolution + ",\"duedate\":\"2026-09-10\",\"timeoriginalestimate\":28800,"
                + "\"timespent\":3600,\"assignee\":{\"emailAddress\":\"Dev@Empresa.com\"}}}";
        }

        // T17 — POST /search/jql paginado por nextPageToken, mapeamento de tipos e campos.
        [Fact]
        public async Task Issues_PaginaPorNextPageToken_MapeiaTiposECampos()
        {
            var handler = new StubHandler(req =>
            {
                var body = handler_body(req);
                return body.Contains("\"nextPageToken\"")
                    ? StubHandler.Json($"{{\"issues\":[{IssueJson("PRU-3", "Bug", "done")},{IssueJson("PRU-4", "Epic")},{IssueJson("PRU-5", "Subtarefa", subtask: true)}],\"isLast\":true}}")
                    : StubHandler.Json($"{{\"issues\":[{IssueJson("PRU-1", "Story")},{IssueJson("PRU-2", "Feature")}],\"nextPageToken\":\"abc\"}}");
            });
            static string handler_body(HttpRequestMessage req) => req.Content!.ReadAsStringAsync().Result;

            var issues = await Provider(handler).GetIssuesAsync(Credenciais, "PRU");

            Assert.Equal(2, handler.Requests.Count);
            Assert.All(handler.Requests, r =>
            {
                Assert.Equal(HttpMethod.Post, r.Method);
                Assert.Equal("https://empresa.atlassian.net/rest/api/3/search/jql", r.RequestUri!.ToString());
            });
            Assert.Contains("\"maxResults\":100", handler.Bodies[0]);
            Assert.Contains("\"nextPageToken\":\"abc\"", handler.Bodies[1]);
            Assert.Equal(new[] { "PRU-1", "PRU-2", "PRU-3", "PRU-5" }, issues.Select(i => i.ExternalId));
            Assert.Equal(new[] { ExternalIssueType.Story, ExternalIssueType.Feature, ExternalIssueType.Bug, ExternalIssueType.Task }, issues.Select(i => i.Type));

            var bug = issues[2];
            Assert.True(bug.Done);
            Assert.Equal(new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc), bug.CompletedAt);
            Assert.Equal(new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc), bug.CreatedAt);
            Assert.Equal(new DateOnly(2026, 9, 10), bug.DueDate);
            Assert.Equal(8m, bug.EstimateHours);
            Assert.Equal("dev@empresa.com", bug.AssigneeEmail);
            Assert.False(issues[0].Done);
            Assert.Null(issues[0].CompletedAt);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.Gone)]
        public async Task Issues_SemSearchJql_UsaSearchAntigoPorStartAt(HttpStatusCode status)
        {
            var handler = new StubHandler(req =>
            {
                if (req.Method == HttpMethod.Post) return new HttpResponseMessage(status);
                return req.RequestUri!.Query.Contains("startAt=0")
                    ? StubHandler.Json($"{{\"startAt\":0,\"total\":2,\"issues\":[{IssueJson("OLD-1", "Task")}]}}")
                    : StubHandler.Json($"{{\"startAt\":1,\"total\":2,\"issues\":[{IssueJson("OLD-2", "Story")}]}}");
            });

            var issues = await Provider(handler).GetIssuesAsync(Credenciais, "OLD");

            Assert.Equal(new[] { "OLD-1", "OLD-2" }, issues.Select(i => i.ExternalId));
            Assert.Equal(3, handler.Requests.Count);
            Assert.StartsWith("https://empresa.atlassian.net/rest/api/3/search?jql=", handler.Requests[1].RequestUri!.ToString());
            Assert.Contains("startAt=1", handler.Requests[2].RequestUri!.Query);
        }

        [Fact]
        public async Task Worklogs_PaginaEConverteSegundosEmHoras()
        {
            var handler = new StubHandler(req => req.RequestUri!.Query.Contains("startAt=0")
                ? StubHandler.Json("""{"startAt":0,"total":2,"worklogs":[{"id":"10","author":{"emailAddress":"Ana@Empresa.com"},"started":"2026-09-03T09:00:00.000-0300","timeSpentSeconds":5400}]}""")
                : StubHandler.Json("""{"startAt":1,"total":2,"worklogs":[{"id":"11","author":{},"started":"2026-09-04T09:00:00.000+0000","timeSpentSeconds":3600}]}"""));

            var worklogs = await Provider(handler).GetWorklogsAsync(Credenciais, "PRU-1");

            Assert.Equal("https://empresa.atlassian.net/rest/api/3/issue/PRU-1/worklog?startAt=0&maxResults=100", handler.Requests[0].RequestUri!.ToString());
            Assert.Equal(2, worklogs.Count);
            Assert.Equal(new ExternalWorklogData("10", "ana@empresa.com", 1.5m, new DateOnly(2026, 9, 3)), worklogs[0]);
            Assert.Null(worklogs[1].AuthorEmail);
        }

        [Fact]
        public async Task Erros_401EhAutenticacao_5xxEhIndisponivel_4xxEhRequisicao()
        {
            await Assert.ThrowsAsync<IntegrationAuthException>(() =>
                Provider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))).GetIssuesAsync(Credenciais, "PRU"));
            await Assert.ThrowsAsync<IntegrationUnavailableException>(() =>
                Provider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))).GetIssuesAsync(Credenciais, "PRU"));
            await Assert.ThrowsAsync<IntegrationUnavailableException>(() =>
                Provider(new StubHandler(_ => throw new HttpRequestException("rede"))).GetWorklogsAsync(Credenciais, "PRU-1"));
            var ex = await Assert.ThrowsAsync<IntegrationRequestException>(() =>
                Provider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest))).GetIssuesAsync(Credenciais, "NAOEXISTE"));
            Assert.Equal(400, ex.StatusCode);
        }
    }
}
