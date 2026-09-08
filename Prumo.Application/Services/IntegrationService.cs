using Microsoft.Extensions.Logging;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Exceptions;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Prumo.Application.Services
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IRepository<Integration> _integrationRepository;
        private readonly IRepository<ExternalData> _externalDataRepository;
        private readonly IIntegrationProviderFactory _providerFactory;
        private readonly ILogger<IntegrationService> _logger;

        public IntegrationService(
            IRepository<Integration> integrationRepository,
            IRepository<ExternalData> externalDataRepository,
            IIntegrationProviderFactory providerFactory,
            ILogger<IntegrationService> logger)
        {
            _integrationRepository = integrationRepository;
            _externalDataRepository = externalDataRepository;
            _providerFactory = providerFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<IntegrationDto>> GetAllAsync()
        {
            var entities = await _integrationRepository.GetAllAsync();
            return entities.Select(MapToDto);
        }

        public async Task<IntegrationDto> GetByIdAsync(Guid id)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IntegrationDto> ConfigureAsync(ConfigureIntegrationDto dto)
        {
            var provider = GetRequiredProvider(dto.Type);

            var validation = await provider.ValidateConnectionAsync(dto.ApiUrl, dto.Token);
            if (!validation.Success)
            {
                throw new IntegrationAuthenticationException(
                    validation.Message ?? "Falha na autenticação com a ferramenta externa.");
            }

            var entity = new Integration
            {
                Type = dto.Type,
                ApiUrl = dto.ApiUrl,
                Token = dto.Token,
                IsActive = true,
                SyncIntervalMinutes = dto.SyncIntervalMinutes > 0 ? dto.SyncIntervalMinutes : 60,
            };

            await _integrationRepository.AddAsync(entity);

            return MapToDto(entity);
        }

        public async Task<IntegrationDto> UpdateAsync(Guid id, UpdateIntegrationDto dto)
        {
            var entity = await _integrationRepository.GetByIdAsync(id);
            if (entity == null) return null;

            entity.ApiUrl = dto.ApiUrl ?? entity.ApiUrl;
            if (!string.IsNullOrWhiteSpace(dto.Token))
            {
                entity.Token = dto.Token;
            }
            entity.IsActive = dto.IsActive;
            entity.SyncIntervalMinutes = dto.SyncIntervalMinutes > 0 ? dto.SyncIntervalMinutes : entity.SyncIntervalMinutes;

            await _integrationRepository.UpdateAsync(entity);

            return MapToDto(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _integrationRepository.DeleteAsync(id);
        }

        public async Task<IntegrationSyncResultDto> SyncAsync(Guid integrationId)
        {
            var integration = await _integrationRepository.GetByIdAsync(integrationId);
            if (integration == null)
            {
                throw new KeyNotFoundException($"Integração {integrationId} não encontrada.");
            }

            var provider = GetRequiredProvider(integration.Type);

            IntegrationSyncResultDto result;
            try
            {
                var options = new IntegrationSyncOptions { SinceUtc = integration.LastSyncedAt };
                result = await provider.GetDataAsync(integration, options);
            }
            catch (IntegrationAuthenticationException)
            {
                // UC16 exception flow: "Token expirado" -> interrompe a sincronização e sinaliza
                // a necessidade de nova autenticação; não agenda retry automático.
                integration.LastSyncStatus = "AuthenticationFailed";
                await _integrationRepository.UpdateAsync(integration);
                _logger.LogWarning("Sincronização da integração {IntegrationId} interrompida: token expirado ou inválido.", integrationId);
                throw;
            }
            catch (IntegrationUnavailableException ex)
            {
                // UC16 exception flow: "API indisponível" -> registra o erro; uma nova tentativa
                // será feita no próximo ciclo da rotina automática (RF51).
                integration.LastSyncStatus = "Unavailable";
                await _integrationRepository.UpdateAsync(integration);
                _logger.LogError(ex, "Falha ao sincronizar integração {IntegrationId}: API externa indisponível.", integrationId);
                throw;
            }

            result.IssuesImported = await UpsertExternalDataAsync(integration.Id, result.Issues, i => i.ExternalId);
            result.WorklogsImported = await UpsertExternalDataAsync(
                integration.Id, result.Worklogs, w => $"{w.IssueExternalId}:{w.UserName}:{w.LoggedAt:O}");

            integration.LastSyncedAt = DateTime.UtcNow;
            integration.LastSyncStatus = "Success";
            await _integrationRepository.UpdateAsync(integration);

            result.IntegrationId = integration.Id;
            result.SyncedAtUtc = integration.LastSyncedAt.Value;

            return result;
        }

        public async Task SyncDueIntegrationsAsync()
        {
            var dueIds = await GetDueIntegrationIdsAsync();

            foreach (var integrationId in dueIds)
            {
                try
                {
                    await SyncAsync(integrationId);
                }
                catch (Exception ex)
                {
                    // Errors are already persisted on the integration's LastSyncStatus; keep processing
                    // the remaining integrations so a single failure doesn't halt the whole routine.
                    _logger.LogError(ex, "Falha na rotina automática de sincronização da integração {IntegrationId}.", integrationId);
                }
            }
        }

        public async Task<IEnumerable<Guid>> GetDueIntegrationIdsAsync()
        {
            var integrations = await _integrationRepository.FindAsync(i => i.IsActive);

            return integrations
                .Where(i => i.LastSyncedAt == null ||
                    DateTime.UtcNow >= i.LastSyncedAt.Value.AddMinutes(i.SyncIntervalMinutes))
                .Select(i => i.Id)
                .ToList();
        }

        private IIntegrationProvider GetRequiredProvider(Domain.Enums.IntegrationType type)
        {
            var provider = _providerFactory.GetProvider(type);
            if (provider == null)
            {
                throw new NotSupportedException(
                    $"Não há um provedor de integração implementado para '{type}'.");
            }
            return provider;
        }

        private async Task<int> UpsertExternalDataAsync<T>(Guid integrationId, IEnumerable<T> items, Func<T, string> externalIdSelector)
        {
            var count = 0;
            foreach (var item in items)
            {
                var externalId = externalIdSelector(item);
                var existing = (await _externalDataRepository.FindAsync(
                    e => e.IntegrationId == integrationId && e.ExternalId == externalId)).FirstOrDefault();

                var rawJson = JsonSerializer.Serialize(item);

                if (existing == null)
                {
                    await _externalDataRepository.AddAsync(new ExternalData
                    {
                        IntegrationId = integrationId,
                        ExternalId = externalId,
                        RawDataJson = rawJson,
                        ImportedAt = DateTime.UtcNow,
                    });
                }
                else
                {
                    existing.RawDataJson = rawJson;
                    existing.ImportedAt = DateTime.UtcNow;
                    await _externalDataRepository.UpdateAsync(existing);
                }

                count++;
            }
            return count;
        }

        private static IntegrationDto MapToDto(Integration entity)
        {
            return new IntegrationDto
            {
                Id = entity.Id,
                Type = entity.Type,
                ApiUrl = entity.ApiUrl,
                IsActive = entity.IsActive,
                SyncIntervalMinutes = entity.SyncIntervalMinutes,
                LastSyncedAt = entity.LastSyncedAt,
                LastSyncStatus = entity.LastSyncStatus,
            };
        }
    }
}
