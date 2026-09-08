using Prumo.Application.DTOs.Integration;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Generic contract that every external project-management tool integration must implement
    /// (Jira, Azure DevOps, GitHub, Trello - RF47/RF48/RF49/RF50). Concrete implementations live
    /// in the Infrastructure layer and are resolved at runtime via <see cref="IIntegrationProviderFactory"/>
    /// based on <see cref="Integration.Type"/>, so the rest of the application (service, controller,
    /// background sync job) never depends on a specific tool's API.
    /// </summary>
    public interface IIntegrationProvider
    {
        /// <summary>The external tool this provider implements.</summary>
        IntegrationType Type { get; }

        /// <summary>
        /// Validates that the informed URL/credentials can authenticate against the external tool
        /// (UC15 "Configurar Integração": the system validates the connection before saving it).
        /// </summary>
        Task<IntegrationConnectionResultDto> ValidateConnectionAsync(
            string apiUrl, string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches issues/tasks/cards, worklogs and estimates from the external tool
        /// (UC16 "Sincronizar Dados": the system queries the external API and obtains
        /// issues, worklogs and estimates). Implementations must throw
        /// <see cref="Exceptions.IntegrationUnavailableException"/> when the external API is
        /// unreachable, and <see cref="Exceptions.IntegrationAuthenticationException"/> when the
        /// token has expired/is invalid, so the caller can react per the use case's exception flows.
        /// </summary>
        Task<IntegrationSyncResultDto> GetDataAsync(
            Integration integration, IntegrationSyncOptions options, CancellationToken cancellationToken = default);
    }
}
