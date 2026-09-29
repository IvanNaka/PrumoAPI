using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Infrastructure.Integrations
{
    /// <summary>
    /// JiraProvider (Jira Cloud, REST API v3). Autenticação: Basic base64(email:apiToken).
    /// </summary>
    public class JiraIntegrationProvider : IIntegrationProvider
    {
        internal const int PageSize = 100;

        internal static readonly string[] Fields =
        {
            "summary", "issuetype", "status", "created", "resolutiondate", "duedate",
            "timeoriginalestimate", "timespent", "assignee", "updated",
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<JiraIntegrationProvider> _logger;

        public JiraIntegrationProvider(HttpClient httpClient, ILogger<JiraIntegrationProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public IntegrationType Type => IntegrationType.Jira;

        /// <summary>GET {url}/rest/api/3/myself — 200 = sucesso; qualquer outra resposta = falha.</summary>
        public async Task<bool> TestConnectionAsync(IntegrationCredentials credentials, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(credentials.ApiToken))
            {
                return false;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, Url(credentials, "/rest/api/3/myself"));
                request.Headers.Authorization = AuthHeader(credentials);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
            {
                _logger.LogWarning("Teste de conexão com o Jira falhou: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// POST /rest/api/3/search/jql paginado por nextPageToken. Se a instância responder 404/410,
        /// usa o antigo GET /rest/api/3/search paginado por startAt.
        /// </summary>
        public async Task<IReadOnlyList<ExternalIssueData>> GetIssuesAsync(IntegrationCredentials credentials, string projectKey, CancellationToken cancellationToken = default)
        {
            var jql = $"project = \"{projectKey.Replace("\"", string.Empty)}\" ORDER BY updated DESC";
            var result = new List<ExternalIssueData>();
            string? nextPageToken = null;

            do
            {
                var body = new Dictionary<string, object>
                {
                    ["jql"] = jql,
                    ["maxResults"] = PageSize,
                    ["fields"] = Fields,
                };
                if (nextPageToken != null)
                {
                    body["nextPageToken"] = nextPageToken;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, Url(credentials, "/rest/api/3/search/jql"))
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
                };
                using var response = await SendAsync(credentials, request, cancellationToken, allowNotFound: true);
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    if (nextPageToken != null)
                    {
                        throw new IntegrationRequestException((int)response.StatusCode, "O Jira interrompeu a paginação da busca de issues.");
                    }

                    return await GetIssuesLegacyAsync(credentials, jql, cancellationToken);
                }

                using var doc = await ReadJsonAsync(response, cancellationToken);
                ReadIssues(doc.RootElement, result);
                nextPageToken = doc.RootElement.TryGetProperty("nextPageToken", out var token) && token.ValueKind == JsonValueKind.String
                    ? token.GetString()
                    : null;
                if (doc.RootElement.TryGetProperty("isLast", out var isLast) && isLast.ValueKind == JsonValueKind.True)
                {
                    nextPageToken = null;
                }
            }
            while (!string.IsNullOrEmpty(nextPageToken));

            return result;
        }

        /// <summary>GET /rest/api/3/issue/{key}/worklog paginado por startAt.</summary>
        public async Task<IReadOnlyList<ExternalWorklogData>> GetWorklogsAsync(IntegrationCredentials credentials, string issueKey, CancellationToken cancellationToken = default)
        {
            var result = new List<ExternalWorklogData>();
            var startAt = 0;
            while (true)
            {
                var path = $"/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/worklog?startAt={startAt}&maxResults={PageSize}";
                using var request = new HttpRequestMessage(HttpMethod.Get, Url(credentials, path));
                using var response = await SendAsync(credentials, request, cancellationToken);
                using var doc = await ReadJsonAsync(response, cancellationToken);
                var root = doc.RootElement;

                var count = 0;
                if (root.TryGetProperty("worklogs", out var worklogs) && worklogs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var w in worklogs.EnumerateArray())
                    {
                        count++;
                        var id = Text(w, "id");
                        var started = ParseDateTime(Text(w, "started"));
                        if (id == null || started == null)
                        {
                            continue;
                        }

                        var seconds = w.TryGetProperty("timeSpentSeconds", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetDecimal() : 0m;
                        var email = w.TryGetProperty("author", out var author) ? Text(author, "emailAddress") : null;
                        result.Add(new ExternalWorklogData(id, email?.Trim().ToLowerInvariant(), seconds / 3600m, DateOnly.FromDateTime(started.Value.DateTime)));
                    }
                }

                var total = root.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0;
                startAt += count;
                if (count == 0 || startAt >= total)
                {
                    return result;
                }
            }
        }

        private async Task<IReadOnlyList<ExternalIssueData>> GetIssuesLegacyAsync(IntegrationCredentials credentials, string jql, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Jira sem /search/jql; usando GET /rest/api/3/search.");
            var result = new List<ExternalIssueData>();
            var startAt = 0;
            while (true)
            {
                var path = $"/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&startAt={startAt}&maxResults={PageSize}&fields={string.Join(',', Fields)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, Url(credentials, path));
                using var response = await SendAsync(credentials, request, cancellationToken);
                using var doc = await ReadJsonAsync(response, cancellationToken);
                var count = ReadIssues(doc.RootElement, result);
                var total = doc.RootElement.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number ? t.GetInt32() : 0;
                startAt += count;
                if (count == 0 || startAt >= total)
                {
                    return result;
                }
            }
        }

        /// <summary>Lê o array "issues"; devolve quantas issues vieram (inclusive as ignoradas).</summary>
        private static int ReadIssues(JsonElement root, List<ExternalIssueData> result)
        {
            if (!root.TryGetProperty("issues", out var issues) || issues.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            var count = 0;
            foreach (var issue in issues.EnumerateArray())
            {
                count++;
                var mapped = MapIssue(issue);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return count;
        }

        internal static ExternalIssueData? MapIssue(JsonElement issue)
        {
            var key = Text(issue, "key");
            if (key == null || !issue.TryGetProperty("fields", out var f) || f.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var type = f.TryGetProperty("issuetype", out var it) ? MapType(Text(it, "name"), it.TryGetProperty("subtask", out var st) && st.ValueKind == JsonValueKind.True) : null;
            if (type == null)
            {
                return null;
            }

            string status = string.Empty;
            var done = false;
            if (f.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Object)
            {
                status = Text(s, "name") ?? string.Empty;
                done = s.TryGetProperty("statusCategory", out var cat) && Text(cat, "key") == "done";
            }

            var created = ParseDateTime(Text(f, "created"))?.UtcDateTime ?? DateTime.UtcNow;
            var updated = ParseDateTime(Text(f, "updated"))?.UtcDateTime ?? created;
            var estimate = f.TryGetProperty("timeoriginalestimate", out var e) && e.ValueKind == JsonValueKind.Number
                ? e.GetDecimal() / 3600m
                : (decimal?)null;
            var assignee = f.TryGetProperty("assignee", out var a) && a.ValueKind == JsonValueKind.Object ? Text(a, "emailAddress") : null;
            DateOnly? due = DateOnly.TryParseExact(Text(f, "duedate"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

            var title = Text(f, "summary") ?? key;
            return new ExternalIssueData(
                key,
                title.Length > 500 ? title[..500] : title,
                type.Value,
                status.Length > 100 ? status[..100] : status,
                done,
                created,
                ParseDateTime(Text(f, "resolutiondate"))?.UtcDateTime,
                due,
                updated,
                estimate,
                assignee?.Trim().ToLowerInvariant());
        }

        /// <summary>Story, Task, Bug e Feature → iguais; subtarefa → Task; Epic e outros → ignorar (null).</summary>
        internal static ExternalIssueType? MapType(string? name, bool subtask)
        {
            if (subtask)
            {
                return ExternalIssueType.Task;
            }

            switch (name?.Trim().ToLowerInvariant())
            {
                case "story":
                case "história":
                case "historia":
                    return ExternalIssueType.Story;
                case "task":
                case "tarefa":
                case "sub-task":
                case "subtask":
                case "subtarefa":
                case "sub-tarefa":
                    return ExternalIssueType.Task;
                case "bug":
                    return ExternalIssueType.Bug;
                case "feature":
                    return ExternalIssueType.Feature;
                default:
                    return null;
            }
        }

        /// <summary>Datas do Jira vêm como 2024-01-15T10:30:00.000+0000.</summary>
        internal static DateTimeOffset? ParseDateTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = Regex.Replace(value, @"([+-]\d{2})(\d{2})$", "$1:$2");
            return DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
                ? result
                : null;
        }

        private async Task<HttpResponseMessage> SendAsync(IntegrationCredentials credentials, HttpRequestMessage request, CancellationToken cancellationToken, bool allowNotFound = false)
        {
            request.Headers.Authorization = AuthHeader(credentials);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IntegrationUnavailableException("Tempo esgotado ao chamar o Jira.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new IntegrationUnavailableException("Falha de rede ao chamar o Jira.", ex);
            }

            if (response.IsSuccessStatusCode || (allowNotFound && response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone))
            {
                return response;
            }

            var status = (int)response.StatusCode;
            response.Dispose();
            if (status == 401)
            {
                throw new IntegrationAuthException("O Jira recusou as credenciais (HTTP 401).");
            }

            if (status >= 500 || status == 429)
            {
                throw new IntegrationUnavailableException($"O Jira respondeu HTTP {status}.");
            }

            throw new IntegrationRequestException(status, $"O Jira respondeu HTTP {status}.");
        }

        private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new IntegrationUnavailableException("Resposta inválida do Jira.", ex);
            }
        }

        private static string? Text(JsonElement element, string property) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        internal static AuthenticationHeaderValue AuthHeader(IntegrationCredentials credentials) =>
            new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Email}:{credentials.ApiToken}")));

        internal static string Url(IntegrationCredentials credentials, string path) =>
            $"{credentials.Url.TrimEnd('/')}{path}";
    }
}
