using System.Net;
using System.Net.Http.Headers;
using System.Text;
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

        internal static AuthenticationHeaderValue AuthHeader(IntegrationCredentials credentials) =>
            new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Email}:{credentials.ApiToken}")));

        internal static string Url(IntegrationCredentials credentials, string path) =>
            $"{credentials.Url.TrimEnd('/')}{path}";
    }
}
