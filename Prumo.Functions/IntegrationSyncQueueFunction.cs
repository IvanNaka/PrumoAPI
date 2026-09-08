using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Prumo.Application.Exceptions;
using Prumo.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace Prumo.Functions
{
    /// <summary>
    /// UC16 "Sincronizar Dados": queue-triggered worker that performs the actual synchronization
    /// for a single integration (fetches issues/worklogs/estimates and upserts them locally).
    /// Triggered either by <see cref="IntegrationSyncTimerFunction"/>'s daily run, or by any other
    /// producer that enqueues an integration ID onto the "integration-sync-queue" queue (e.g. a
    /// manual re-sync request). Exceptions are re-thrown so the Functions host applies the
    /// storage queue's built-in retry/poison-queue policy (RF51 exception flow: "API indisponível"
    /// -> nova tentativa automática; "Token expirado" -> mensagem vai para a poison queue após as
    /// tentativas se esgotarem, sinalizando a necessidade de reautenticação).
    /// </summary>
    public class IntegrationSyncQueueFunction
    {
        private readonly IIntegrationService _integrationService;
        private readonly ILogger<IntegrationSyncQueueFunction> _logger;

        public IntegrationSyncQueueFunction(IIntegrationService integrationService, ILogger<IntegrationSyncQueueFunction> logger)
        {
            _integrationService = integrationService;
            _logger = logger;
        }

        [Function("IntegrationSyncQueueFunction")]
        public async Task Run([QueueTrigger("integration-sync-queue")] string integrationIdMessage)
        {
            if (!Guid.TryParse(integrationIdMessage, out var integrationId))
            {
                _logger.LogError("Mensagem inválida na fila de sincronização: '{Message}' não é um Guid.", integrationIdMessage);
                return;
            }

            _logger.LogInformation("Iniciando sincronização da integração {IntegrationId}.", integrationId);

            try
            {
                var result = await _integrationService.SyncAsync(integrationId);
                _logger.LogInformation(
                    "Integração {IntegrationId} sincronizada: {Issues} issue(s), {Worklogs} worklog(s).",
                    integrationId, result.IssuesImported, result.WorklogsImported);
            }
            catch (IntegrationAuthenticationException ex)
            {
                _logger.LogError(ex, "Integração {IntegrationId}: token expirado/inválido. É necessário reconfigurar a integração.", integrationId);
                // Not rethrown: re-authentication requires human action, so retrying automatically
                // would not help. The failure is already persisted in Integration.LastSyncStatus.
            }
            catch (IntegrationUnavailableException ex)
            {
                _logger.LogWarning(ex, "Integração {IntegrationId}: API externa indisponível, a mensagem será reprocessada.", integrationId);
                throw; // let the queue trigger retry per host.json policy
            }
        }
    }
}
