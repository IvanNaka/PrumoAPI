using Prumo.Application.DTOs.Integration;
using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// IIntegracaoProvider: contrato de cada ferramenta externa (Jira completo; Azure DevOps, GitHub
    /// e Trello ficam como trabalho futuro — D09). Resolvido por <see cref="IIntegrationProviderFactory"/>.
    /// O provider só lê dados da ferramenta; o upsert, os estados e o log da sincronização ficam no
    /// <see cref="IIntegrationSyncService"/>.
    /// </summary>
    public interface IIntegrationProvider
    {
        IntegrationType Type { get; }

        /// <summary>TestarConexao: true quando a ferramenta aceita as credenciais.</summary>
        Task<bool> TestConnectionAsync(IntegrationCredentials credentials, CancellationToken cancellationToken = default);

        /// <summary>
        /// Todas as issues do projeto externo (tipos não mapeados, como Epic, já vêm filtrados).
        /// Lança <see cref="IntegrationAuthException"/>, <see cref="IntegrationUnavailableException"/> ou
        /// <see cref="IntegrationRequestException"/>.
        /// </summary>
        Task<IReadOnlyList<ExternalIssueData>> GetIssuesAsync(IntegrationCredentials credentials, string projectKey, CancellationToken cancellationToken = default);

        /// <summary>Todos os worklogs da issue.</summary>
        Task<IReadOnlyList<ExternalWorklogData>> GetWorklogsAsync(IntegrationCredentials credentials, string issueKey, CancellationToken cancellationToken = default);
    }
}
