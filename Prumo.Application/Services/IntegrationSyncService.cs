using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    /// <summary>
    /// Sincronização com o Jira (RF47, RF51, UC16, Seção 3.4.4): upsert de issues e worklogs,
    /// estados, novas tentativas (RN25), token expirado (RN26) e SincronizacaoLog em todos os casos.
    /// </summary>
    public class IntegrationSyncService : IIntegrationSyncService
    {
        /// <summary>RN25: 5 min × tentativas; depois de 3 falhas, só na próxima janela normal.</summary>
        public const int MinutosEntreTentativas = 5;
        public const int MaximoTentativas = 3;

        private readonly IAppDbContext _db;
        private readonly IIntegrationProviderFactory _providers;
        private readonly ISecretProtector _protector;
        private readonly ISyncTrigger _trigger;
        private readonly IAdminNotifier _adminNotifier;
        private readonly IEnumerable<ISyncCompletedHandler> _completedHandlers;
        private readonly ILogger<IntegrationSyncService> _logger;

        public IntegrationSyncService(
            IAppDbContext db,
            IIntegrationProviderFactory providers,
            ISecretProtector protector,
            ISyncTrigger trigger,
            IAdminNotifier adminNotifier,
            IEnumerable<ISyncCompletedHandler> completedHandlers,
            ILogger<IntegrationSyncService> logger)
        {
            _db = db;
            _providers = providers;
            _protector = protector;
            _trigger = trigger;
            _adminNotifier = adminNotifier;
            _completedHandlers = completedHandlers;
            _logger = logger;
        }

        public async Task<IntegracaoJiraDto> RequestJiraSyncAsync()
        {
            var integration = await _db.Integrations.SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            var status = integration?.Status ?? IntegrationStatus.NaoConfigurada;
            var evento = EventoInicio(status);
            integration!.Status = IntegrationStateMachine.Aplicar(status, evento);
            await _db.SaveChangesAsync();

            _trigger.Trigger();
            return IntegrationService.Map(integration);
        }

        public async Task<bool> IsJiraSyncDueAsync(DateTime agora)
        {
            var integration = await _db.Integrations.AsNoTracking().SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            if (integration == null || !integration.IsActive)
            {
                return false;
            }

            return integration.Status switch
            {
                IntegrationStatus.Conectada => integration.LastSyncedAt == null
                    || agora >= integration.LastSyncedAt.Value.AddMinutes(integration.SyncIntervalMinutes),
                IntegrationStatus.FalhaSincronizacao => integration.NextAttemptAt == null || agora >= integration.NextAttemptAt,
                _ => false,
            };
        }

        public async Task RecoverInterruptedSyncAsync()
        {
            var integration = await _db.Integrations.SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            if (integration?.Status == IntegrationStatus.Sincronizando)
            {
                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.SyncErro);
                integration.NextAttemptAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task RunJiraSyncAsync(CancellationToken cancellationToken)
        {
            var integration = await _db.Integrations.SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira, cancellationToken);
            if (integration == null)
            {
                return;
            }

            // A sincronização manual já deixou o estado em Sincronizando; a agendada faz a transição aqui.
            if (integration.Status != IntegrationStatus.Sincronizando)
            {
                if (!IntegrationStateMachine.Permite(integration.Status, EventoInicioOuNulo(integration.Status) ?? string.Empty))
                {
                    return;
                }

                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, EventoInicio(integration.Status));
                await _db.SaveChangesAsync(cancellationToken);
            }

            var log = new IntegrationSyncLog { IntegrationId = integration.Id, StartedAt = DateTime.UtcNow };
            var avisos = new List<string>();
            try
            {
                var provider = _providers.GetProvider(integration.Type)
                    ?? throw new InvalidOperationException("Integração planejada para versão futura.");
                var credentials = IntegrationService.Credentials(integration, _protector, _logger);
                if (string.IsNullOrEmpty(credentials.ApiToken))
                {
                    throw new IntegrationAuthException("Token do Jira indisponível.");
                }

                var projects = await _db.Projects.AsNoTracking()
                    .Where(p => p.JiraProjectKey != null && p.JiraProjectKey != "")
                    .Select(p => new { p.Id, p.Name, Key = p.JiraProjectKey! })
                    .ToListAsync(cancellationToken);

                foreach (var project in projects)
                {
                    IReadOnlyList<ExternalIssueData> issues;
                    try
                    {
                        issues = await provider.GetIssuesAsync(credentials, project.Key.Trim(), cancellationToken);
                    }
                    catch (IntegrationRequestException ex)
                    {
                        // Chave inexistente ou sem permissão: registra e segue com os demais projetos.
                        avisos.Add($"Projeto {project.Name} (chave {project.Key}): {ex.Message}");
                        continue;
                    }

                    var (issuesCount, worklogsCount) = await UpsertAsync(provider, credentials, project.Id, issues, cancellationToken);
                    log.IssuesProcessed += issuesCount;
                    log.WorklogsProcessed += worklogsCount;
                }

                // Sincronizando -> Conectada.
                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.SyncSucesso);
                integration.LastSyncedAt = DateTime.UtcNow;
                integration.FailedAttempts = 0;
                integration.NextAttemptAt = null;
                log.Success = true;
                log.ErrorMessage = avisos.Count > 0 ? Truncate(string.Join(" | ", avisos)) : null;
                await FinishAsync(log);
            }
            catch (IntegrationAuthException ex)
            {
                // RN26: 401 -> ErroConexao e aviso aos Administradores.
                _logger.LogWarning("Sincronização com o Jira recusada: {Message}", ex.Message);
                integration = await ResetAsync(integration.Id);
                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.TokenExpirado);
                integration.NextAttemptAt = null;
                log.ErrorMessage = Messages.RN26_TokenJiraExpirado;
                await FinishAsync(log);
                await _adminNotifier.NotifyAdminsAsync(Messages.RN26_TokenJiraExpirado, CancellationToken.None);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // API sendo desligada: tenta de novo na próxima subida.
                integration = await ResetAsync(integration.Id);
                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.SyncErro);
                integration.NextAttemptAt = DateTime.UtcNow;
                log.ErrorMessage = "Sincronização interrompida.";
                await FinishAsync(log);
                throw;
            }
            catch (Exception ex)
            {
                // RN25: HTTP 5xx, timeout ou outro erro -> FalhaSincronizacao e nova tentativa agendada.
                _logger.LogWarning(ex, Messages.RN25_JiraIndisponivel);
                integration = await ResetAsync(integration.Id);
                integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.SyncErro);
                integration.FailedAttempts++;
                var agora = DateTime.UtcNow;
                integration.NextAttemptAt = integration.FailedAttempts >= MaximoTentativas
                    ? agora.AddMinutes(integration.SyncIntervalMinutes)
                    : agora.AddMinutes(MinutosEntreTentativas * integration.FailedAttempts);
                log.ErrorMessage = Truncate($"{Messages.RN25_JiraIndisponivel} {ex.Message}");
                await FinishAsync(log);
                return;
            }

            foreach (var handler in _completedHandlers)
            {
                try
                {
                    await handler.OnSyncCompletedAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Falha ao executar {Handler} depois da sincronização.", handler.GetType().Name);
                }
            }
        }

        /// <summary>Upsert por IdExterno; worklogs só das issues novas ou alteradas.</summary>
        private async Task<(int Issues, int Worklogs)> UpsertAsync(
            IIntegrationProvider provider,
            IntegrationCredentials credentials,
            Guid projectId,
            IReadOnlyList<ExternalIssueData> issues,
            CancellationToken cancellationToken)
        {
            var keys = issues.Select(i => i.ExternalId).Distinct().ToList();
            var existing = await _db.Issues
                .Include(i => i.Worklogs)
                .Where(i => keys.Contains(i.ExternalId))
                .ToDictionaryAsync(i => i.ExternalId, cancellationToken);

            var worklogsCount = 0;
            foreach (var data in issues.GroupBy(i => i.ExternalId).Select(g => g.First()))
            {
                var isNew = !existing.TryGetValue(data.ExternalId, out var issue);
                var changed = isNew || issue!.UpdatedAt != data.UpdatedAt || issue.ProjectId != projectId;
                if (isNew)
                {
                    issue = new ExternalIssue { ExternalId = data.ExternalId, Source = "Jira" };
                    _db.Issues.Add(issue);
                }

                issue!.ProjectId = projectId;
                issue.Title = data.Title;
                issue.Type = data.Type;
                issue.Status = data.Status;
                issue.Done = data.Done;
                issue.EstimateHours = data.EstimateHours;
                issue.AssigneeEmail = data.AssigneeEmail;
                issue.CreatedAt = data.CreatedAt;
                issue.CompletedAt = data.CompletedAt;
                issue.DueDate = data.DueDate;
                issue.UpdatedAt = data.UpdatedAt;

                if (!changed)
                {
                    continue;
                }

                var worklogs = await provider.GetWorklogsAsync(credentials, data.ExternalId, cancellationToken);
                worklogsCount += worklogs.Count;
                await UpsertWorklogsAsync(issue, worklogs, cancellationToken);
                issue.SpentHours = Math.Round(worklogs.Sum(w => w.Hours), 2);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return (keys.Count, worklogsCount);
        }

        private async Task UpsertWorklogsAsync(ExternalIssue issue, IReadOnlyList<ExternalWorklogData> worklogs, CancellationToken cancellationToken)
        {
            var ids = worklogs.Select(w => w.ExternalId).Distinct().ToList();

            // Worklog que mudou de issue no Jira: é o mesmo registro (IdExterno único).
            var existing = await _db.Worklogs
                .Where(w => ids.Contains(w.ExternalId))
                .ToDictionaryAsync(w => w.ExternalId, cancellationToken);
            foreach (var w in issue.Worklogs.Where(w => w.ExternalId.Length > 0))
            {
                existing.TryAdd(w.ExternalId, w);
            }

            // Worklogs apagados no Jira saem também do Prumo.
            foreach (var removed in issue.Worklogs.Where(w => !ids.Contains(w.ExternalId)).ToList())
            {
                _db.Worklogs.Remove(removed);
            }

            foreach (var data in worklogs.GroupBy(w => w.ExternalId).Select(g => g.First()))
            {
                if (!existing.TryGetValue(data.ExternalId, out var worklog))
                {
                    worklog = new ExternalWorklog { ExternalId = data.ExternalId, IssueId = issue.Id };
                    _db.Worklogs.Add(worklog);
                }

                worklog.IssueId = issue.Id;
                worklog.AuthorEmail = data.AuthorEmail;
                worklog.Hours = Math.Round(data.Hours, 2);
                worklog.Date = data.Date;
            }
        }

        /// <summary>Descarta o que ficou pela metade e recarrega a integração.</summary>
        private async Task<Integration> ResetAsync(Guid integrationId)
        {
            _db.DiscardChanges();
            return await _db.Integrations.SingleAsync(i => i.Id == integrationId);
        }

        private async Task FinishAsync(IntegrationSyncLog log)
        {
            log.FinishedAt = DateTime.UtcNow;
            _db.IntegrationSyncLogs.Add(log);
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        private static string EventoInicio(IntegrationStatus status) =>
            EventoInicioOuNulo(status) ?? IntegrationStateMachine.Sincronizar;

        private static string? EventoInicioOuNulo(IntegrationStatus status) => status switch
        {
            IntegrationStatus.Conectada => IntegrationStateMachine.Sincronizar,
            IntegrationStatus.FalhaSincronizacao => IntegrationStateMachine.TentarNovamente,
            _ => null,
        };

        private static string Truncate(string value) => value.Length > 2000 ? value[..2000] : value;
    }
}
