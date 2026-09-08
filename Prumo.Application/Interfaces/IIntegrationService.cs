using Prumo.Application.DTOs.Integration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Orchestrates integration configuration (UC15) and data synchronization (UC16) against
    /// whichever external tool is configured, using <see cref="IIntegrationProvider"/> under the hood.
    /// </summary>
    public interface IIntegrationService
    {
        Task<IEnumerable<IntegrationDto>> GetAllAsync();
        Task<IntegrationDto> GetByIdAsync(Guid id);

        /// <summary>
        /// UC15 "Configurar Integração": validates the connection and, if valid, persists the
        /// configuration. Throws <see cref="Exceptions.IntegrationAuthenticationException"/> when
        /// the credentials are rejected.
        /// </summary>
        Task<IntegrationDto> ConfigureAsync(ConfigureIntegrationDto dto);

        Task<IntegrationDto> UpdateAsync(Guid id, UpdateIntegrationDto dto);

        Task DeleteAsync(Guid id);

        /// <summary>
        /// UC16 "Sincronizar Dados": queries the external tool for issues/worklogs/estimates and
        /// upserts them locally (as <see cref="Domain.Entities.ExternalData"/>). On completion,
        /// updates <see cref="Domain.Entities.Integration.LastSyncedAt"/> and
        /// <see cref="Domain.Entities.Integration.LastSyncStatus"/>.
        /// </summary>
        Task<IntegrationSyncResultDto> SyncAsync(Guid integrationId);

        /// <summary>
        /// RF51 / Cenário 6 "Rotina de Sincronização Automática": syncs every active integration
        /// whose configured interval has elapsed since its last sync. Failures for one integration
        /// do not stop the others (each is logged via <see cref="Domain.Entities.Integration.LastSyncStatus"/>).
        /// </summary>
        Task SyncDueIntegrationsAsync();

        /// <summary>
        /// Returns the IDs of active integrations whose configured sync interval
        /// (<see cref="Domain.Entities.Integration.SyncIntervalMinutes"/>) has elapsed since the last
        /// successful sync. Used by the daily Azure Function timer trigger to enqueue one sync
        /// message per due integration onto the sync queue (RF51).
        /// </summary>
        Task<IEnumerable<Guid>> GetDueIntegrationIdsAsync();
    }
}
