using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Infrastructure.Integrations
{
    /// <summary>
    /// Azure DevOps, GitHub e Trello (RF48–RF50) ficam como trabalho futuro (D09): implementam o contrato
    /// <see cref="IIntegrationProvider"/>, mas não são exibidos na interface.
    /// </summary>
    public abstract class FutureIntegrationProvider : IIntegrationProvider
    {
        public const string Mensagem = "Integração planejada para versão futura.";

        public abstract IntegrationType Type { get; }

        public Task<bool> TestConnectionAsync(IntegrationCredentials credentials, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(Mensagem);

        public Task<IReadOnlyList<ExternalIssueData>> GetIssuesAsync(IntegrationCredentials credentials, string projectKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(Mensagem);

        public Task<IReadOnlyList<ExternalWorklogData>> GetWorklogsAsync(IntegrationCredentials credentials, string issueKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException(Mensagem);
    }

    /// <summary>RF48 — referência futura: WIQL em POST https://dev.azure.com/{org}/{proj}/_apis/wit/wiql?api-version=7.1 e depois workitemsbatch.</summary>
    public class AzureDevOpsProvider : FutureIntegrationProvider
    {
        public override IntegrationType Type => IntegrationType.AzureDevOps;
    }

    /// <summary>RF49 — referência futura: GET /repos/{owner}/{repo}/pulls?state=closed e /issues.</summary>
    public class GitHubProvider : FutureIntegrationProvider
    {
        public override IntegrationType Type => IntegrationType.GitHub;
    }

    /// <summary>RF50 — referência futura: GET /1/boards/{id}/cards.</summary>
    public class TrelloProvider : FutureIntegrationProvider
    {
        public override IntegrationType Type => IntegrationType.Trello;
    }
}
