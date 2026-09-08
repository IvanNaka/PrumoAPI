using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Prumo.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Prumo.Functions
{
    /// <summary>
    /// RF51 / US11 "Cenário 6: Rotina de Sincronização Automática": runs once a day and enqueues
    /// one sync message per active integration whose configured interval
    /// (<c>SyncIntervalMinutes</c>) has elapsed since its last successful sync. The actual sync
    /// work is performed by <see cref="IntegrationSyncQueueFunction"/>, which is triggered by each
    /// message — this keeps the daily schedule decoupled from the (possibly slow/unreliable)
    /// external API calls, and gives automatic retry/poison-queue handling per integration.
    /// </summary>
    public class IntegrationSyncTimerFunction
    {
        private readonly IIntegrationService _integrationService;
        private readonly ILogger<IntegrationSyncTimerFunction> _logger;

        public IntegrationSyncTimerFunction(IIntegrationService integrationService, ILogger<IntegrationSyncTimerFunction> logger)
        {
            _integrationService = integrationService;
            _logger = logger;
        }

        /// <summary>Runs daily at 03:00 UTC. Schedule is a standard 6-field NCRONTAB expression.</summary>
        [Function("IntegrationSyncTimerFunction")]
        [QueueOutput("integration-sync-queue")]
        public async Task<string[]> Run([TimerTrigger("0 0 3 * * *")] TimerInfo timer)
        {
            var dueIntegrationIds = (await _integrationService.GetDueIntegrationIdsAsync()).ToList();

            _logger.LogInformation(
                "Rotina diária de sincronização: {Count} integração(ões) elegível(is) para sincronização.",
                dueIntegrationIds.Count);

            return dueIntegrationIds.Select(id => id.ToString()).ToArray();
        }
    }
}
