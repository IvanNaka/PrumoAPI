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
    // Integração Jira — configuração e teste de conexão (RF47, UC15, Figura 29).
    public class IntegrationService : IIntegrationService
    {
        private readonly IAppDbContext _db;
        private readonly IIntegrationProviderFactory _providers;
        private readonly ISecretProtector _protector;
        private readonly ILogger<IntegrationService> _logger;

        public IntegrationService(
            IAppDbContext db,
            IIntegrationProviderFactory providers,
            ISecretProtector protector,
            ILogger<IntegrationService> logger)
        {
            _db = db;
            _providers = providers;
            _protector = protector;
            _logger = logger;
        }

        public async Task<IntegracaoJiraDto> GetJiraAsync()
        {
            var integration = await _db.Integrations.AsNoTracking().SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            return Map(integration);
        }

        public async Task<IntegracaoJiraDto> SaveJiraAsync(SalvarIntegracaoJiraDto dto)
        {
            var integration = await _db.Integrations.SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            var isNew = integration == null;
            integration ??= new Integration { Type = IntegrationType.Jira, Status = IntegrationStatus.NaoConfigurada };

            var url = dto.Url?.Trim().TrimEnd('/') ?? string.Empty;
            var email = dto.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            var token = dto.ApiToken?.Trim();
            var errors = new Dictionary<string, string[]>();
            if (url.Length == 0) errors["url"] = new[] { "A URL é obrigatória." };
            if (email.Length == 0) errors["email"] = new[] { "O e-mail é obrigatório." };
            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(integration.Token)) errors["apiToken"] = new[] { "O API token é obrigatório." };
            if (errors.Count > 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios, errors);
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                throw new BusinessRuleException(400, "Informe uma URL válida, por exemplo https://empresa.atlassian.net.");
            }

            var intervalo = dto.IntervaloSincronizacaoMinutos ?? integration.SyncIntervalMinutes;
            if (intervalo is < 15 or > 1440)
            {
                throw new BusinessRuleException(400, "O intervalo de sincronização deve ficar entre 15 e 1440 minutos.");
            }

            // Salvar credenciais -> Configurada (Figura 29).
            integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.Salvar);
            integration.ApiUrl = url;
            integration.Email = email;
            if (!string.IsNullOrEmpty(token))
            {
                integration.Token = _protector.Protect(token);
            }
            integration.SyncIntervalMinutes = intervalo;
            integration.IsActive = dto.Ativo ?? true;
            integration.FailedAttempts = 0;
            integration.NextAttemptAt = null;
            integration.UpdatedDate = DateTime.UtcNow;

            if (isNew)
            {
                _db.Integrations.Add(integration);
            }

            await _db.SaveChangesAsync();

            // Em seguida testa automaticamente; em caso de falha a configuração continua salva (ErroConexao).
            await RunConnectionTestAsync(integration);
            return Map(integration);
        }

        public async Task<IntegracaoJiraDto> TestJiraAsync()
        {
            var integration = await _db.Integrations.SingleOrDefaultAsync(i => i.Type == IntegrationType.Jira);
            if (integration == null || integration.Status == IntegrationStatus.NaoConfigurada)
            {
                throw new BusinessRuleException(409, Messages.RN22_Transicao(IntegrationStatus.NaoConfigurada, IntegrationStateMachine.Testar));
            }

            await RunConnectionTestAsync(integration);
            return Map(integration);
        }

        public async Task<IEnumerable<SincronizacaoLogDto>> GetJiraLogsAsync(int limit = 50)
        {
            return await _db.IntegrationSyncLogs.AsNoTracking()
                .Where(l => l.Integration.Type == IntegrationType.Jira)
                .OrderByDescending(l => l.StartedAt)
                .Take(limit)
                .Select(l => new SincronizacaoLogDto
                {
                    Id = l.Id,
                    Inicio = l.StartedAt,
                    Fim = l.FinishedAt,
                    Sucesso = l.Success,
                    IssuesProcessadas = l.IssuesProcessed,
                    WorklogsProcessados = l.WorklogsProcessed,
                    MensagemErro = l.ErrorMessage,
                })
                .ToListAsync();
        }

        /// <summary>Configurada -> TestandoConexao -> Conectada | ErroConexao. Falha: RN24 (400).</summary>
        private async Task RunConnectionTestAsync(Integration integration)
        {
            integration.Status = IntegrationStateMachine.Aplicar(integration.Status, IntegrationStateMachine.Testar);
            await _db.SaveChangesAsync();

            bool ok;
            try
            {
                var provider = _providers.GetProvider(integration.Type)
                    ?? throw new BusinessRuleException(400, "Integração planejada para versão futura.");
                ok = await provider.TestConnectionAsync(Credentials(integration, _protector, _logger));
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao testar a conexão com o Jira.");
                ok = false;
            }

            integration.Status = IntegrationStateMachine.Aplicar(integration.Status,
                ok ? IntegrationStateMachine.Sucesso : IntegrationStateMachine.Falha);
            await _db.SaveChangesAsync();

            if (!ok)
            {
                throw new BusinessRuleException(400, Messages.RN24_FalhaJira);
            }
        }

        internal static IntegrationCredentials Credentials(Integration integration, ISecretProtector protector, ILogger logger)
        {
            string token;
            try
            {
                token = protector.Unprotect(integration.Token);
            }
            catch (Exception ex)
            {
                // Token antigo sem criptografia ou chave perdida: força nova autenticação.
                logger.LogWarning(ex, "Não foi possível descriptografar o token do Jira.");
                token = string.Empty;
            }

            return new IntegrationCredentials(integration.ApiUrl, integration.Email, token);
        }

        internal static IntegracaoJiraDto Map(Integration? integration) => integration == null
            ? new IntegracaoJiraDto()
            : new IntegracaoJiraDto
            {
                Configurada = integration.Status != IntegrationStatus.NaoConfigurada,
                Url = integration.ApiUrl,
                Email = integration.Email,
                IntervaloSincronizacaoMinutos = integration.SyncIntervalMinutes,
                Ativo = integration.IsActive,
                Status = integration.Status.ToString(),
                UltimaSincronizacao = integration.LastSyncedAt,
                TentativasFalhas = integration.FailedAttempts,
                ProximaTentativa = integration.NextAttemptAt,
                TokenConfigurado = !string.IsNullOrEmpty(integration.Token),
            };
    }
}
