using Microsoft.Extensions.Logging;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Exceptions;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Prumo.Infrastructure.Integrations
{
    /// <summary>
    /// Reference implementation of <see cref="IIntegrationProvider"/> for Jira Cloud, following
    /// UC15/UC16 (Configurar Integração Jira / Sincronizar Dados do Jira): validates the connection
    /// via GET /rest/api/3/myself, then fetches issues (with type/status/estimate) via
    /// GET /rest/api/3/search and their worklogs via GET /rest/api/3/issue/{key}/worklog.
    /// Authentication uses Jira's API token Basic auth scheme ("email:apiToken" -> Base64), where
    /// the "email:apiToken" pair is expected to be stored in <see cref="Integration.Token"/>.
    /// </summary>
    public class JiraIntegrationProvider : IIntegrationProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<JiraIntegrationProvider> _logger;

        public IntegrationType Type => IntegrationType.Jira;

        public JiraIntegrationProvider(HttpClient httpClient, ILogger<JiraIntegrationProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IntegrationConnectionResultDto> ValidateConnectionAsync(
            string apiUrl, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, CombineUrl(apiUrl, "/rest/api/3/myself"));
                request.Headers.Authorization = BuildAuthHeader(token);

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new IntegrationConnectionResultDto
                    {
                        Success = false,
                        Message = "Credenciais inválidas ou expiradas para a integração com o Jira.",
                    };
                }

                return new IntegrationConnectionResultDto
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode
                        ? "Conexão com o Jira validada com sucesso."
                        : $"Falha ao validar a conexão com o Jira (HTTP {(int)response.StatusCode}).",
                };
            }
            catch (HttpRequestException ex)
            {
                return new IntegrationConnectionResultDto
                {
                    Success = false,
                    Message = $"Não foi possível conectar à API do Jira: {ex.Message}",
                };
            }
        }

        public async Task<IntegrationSyncResultDto> GetDataAsync(
            Integration integration, IntegrationSyncOptions options, CancellationToken cancellationToken = default)
        {
            var result = new IntegrationSyncResultDto();

            var jql = options?.SinceUtc != null
                ? $"updated >= \"{options.SinceUtc:yyyy/MM/dd HH:mm}\""
                : string.Empty;

            var searchUrl = CombineUrl(integration.ApiUrl,
                $"/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&maxResults=100&fields=summary,issuetype,status,project,assignee,timeoriginalestimate,created,resolutiondate");

            using var response = await SendAuthenticatedAsync(HttpMethod.Get, searchUrl, integration.Token, cancellationToken);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("issues", out var issuesElement))
            {
                return result;
            }

            foreach (var issue in issuesElement.EnumerateArray())
            {
                var fields = issue.GetProperty("fields");
                var externalId = issue.GetProperty("key").GetString();

                var issueDto = new ExternalIssueDto
                {
                    ExternalId = externalId,
                    Key = externalId,
                    Title = GetString(fields, "summary"),
                    Type = MapIssueType(GetNestedString(fields, "issuetype", "name")),
                    Status = GetNestedString(fields, "status", "name"),
                    ProjectExternalId = GetNestedString(fields, "project", "key"),
                    AssigneeName = GetNestedString(fields, "assignee", "displayName"),
                    EstimateHours = GetOptionalSeconds(fields, "timeoriginalestimate"),
                    CreatedAt = GetDate(fields, "created") ?? DateTime.UtcNow,
                    ResolvedAt = GetDate(fields, "resolutiondate"),
                };

                if (issueDto.Type == null)
                {
                    continue;
                }

                result.Issues.Add(issueDto);

                foreach (var worklog in await GetWorklogsAsync(integration, externalId, cancellationToken))
                {
                    result.Worklogs.Add(worklog);
                }
            }

            return result;
        }

        private async Task<List<ExternalWorklogDto>> GetWorklogsAsync(
            Integration integration, string issueKey, CancellationToken cancellationToken)
        {
            var worklogs = new List<ExternalWorklogDto>();

            try
            {
                var worklogUrl = CombineUrl(integration.ApiUrl, $"/rest/api/3/issue/{issueKey}/worklog");
                using var response = await SendAuthenticatedAsync(HttpMethod.Get, worklogUrl, integration.Token, cancellationToken);

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (!document.RootElement.TryGetProperty("worklogs", out var worklogsElement))
                {
                    return worklogs;
                }

                foreach (var worklog in worklogsElement.EnumerateArray())
                {
                    worklogs.Add(new ExternalWorklogDto
                    {
                        IssueExternalId = issueKey,
                        UserName = GetNestedString(worklog, "author", "displayName"),
                        HoursSpent = worklog.TryGetProperty("timeSpentSeconds", out var seconds) ? seconds.GetDouble() / 3600.0 : 0,
                        LoggedAt = GetDate(worklog, "started") ?? DateTime.UtcNow,
                    });
                }
            }
            catch (IntegrationUnavailableException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao obter worklogs da issue {IssueKey} no Jira; prosseguindo sem eles.", issueKey);
            }

            return worklogs;
        }

        private async Task<HttpResponseMessage> SendAuthenticatedAsync(
            HttpMethod method, string url, string token, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = BuildAuthHeader(token);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new IntegrationUnavailableException("A API do Jira está indisponível no momento.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IntegrationUnavailableException("Tempo limite excedido ao consultar a API do Jira.", ex);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new IntegrationAuthenticationException("Token do Jira expirado ou inválido.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new IntegrationUnavailableException($"A API do Jira retornou HTTP {(int)response.StatusCode}.");
            }

            return response;
        }

        private static AuthenticationHeaderValue BuildAuthHeader(string token)
        {
            // Jira Cloud expects Basic auth with "email:apiToken" encoded in Base64.
            var bytes = Encoding.UTF8.GetBytes(token ?? string.Empty);
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        private static string CombineUrl(string baseUrl, string relativePath)
        {
            return $"{baseUrl?.TrimEnd('/')}{relativePath}";
        }

        private static string GetString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string GetNestedString(JsonElement element, string property, string nestedProperty)
        {
            if (!element.TryGetProperty(property, out var nested) || nested.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            return GetString(nested, nestedProperty);
        }

        private static DateTime? GetDate(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            return DateTime.TryParse(value.GetString(), out var parsed) ? parsed : null;
        }

        private static double? GetOptionalSeconds(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number)
            {
                return null;
            }
            return value.GetDouble() / 3600.0;
        }

        // Story, Task e Bug -> iguais; Feature -> Feature; Sub-task/Subtarefa -> Task;
        // Epic e demais tipos -> ignorados (null).
        private static ExternalIssueType? MapIssueType(string jiraTypeName)
        {
            return jiraTypeName?.Trim().ToLowerInvariant() switch
            {
                "story" or "história" or "historia" => ExternalIssueType.Story,
                "task" or "tarefa" => ExternalIssueType.Task,
                "sub-task" or "subtask" or "subtarefa" => ExternalIssueType.Task,
                "feature" or "funcionalidade" => ExternalIssueType.Feature,
                "bug" => ExternalIssueType.Bug,
                _ => null,
            };
        }
    }
}
