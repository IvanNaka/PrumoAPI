using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Notification;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // NotificacaoService (RF45, RF46, Figura 30). Cada usuário só vê as próprias notificações.
    public class NotificationService : INotificationService
    {
        public const int DiasParaIgnorar = 7;
        public const int DiasParaArquivar = 30;
        public static readonly TimeSpan JanelaDeduplicacao = TimeSpan.FromHours(24);

        private static readonly AlertStatus[] StatusPadrao = { AlertStatus.Enviada, AlertStatus.Lida, AlertStatus.Ignorada };

        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationQueue _queue;

        public NotificationService(IAppDbContext db, ICurrentUserService currentUser, INotificationQueue queue)
        {
            _db = db;
            _currentUser = currentUser;
            _queue = queue;
        }

        public async Task<bool> CreateAsync(Guid userId, AlertType type, string message, string? entityType, Guid? entityId, CancellationToken cancellationToken = default)
        {
            var limite = DateTime.UtcNow - JanelaDeduplicacao;
            var duplicada = await _db.Alerts.AnyAsync(a =>
                a.UserId == userId && a.Type == type && a.EntityId == entityId &&
                a.Status != AlertStatus.Arquivada && a.CreatedDate >= limite, cancellationToken);
            if (duplicada)
            {
                return false;
            }

            var alert = new Alert
            {
                UserId = userId,
                Type = type,
                Message = message.Length > 500 ? message[..500] : message,
                EntityType = entityType,
                EntityId = entityId,
                Status = AlertStatus.Gerada,
            };
            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync(cancellationToken);

            // Gerada -> Enfileirada: entra na fila do dispatcher.
            alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Enfileirar);
            await _db.SaveChangesAsync(cancellationToken);
            _queue.Enqueue(alert.Id);
            return true;
        }

        public async Task<IEnumerable<NotificacaoDto>> ListMineAsync(string? status, string? tipo)
        {
            var userId = _currentUser.RequireUserId();
            var query = _db.Alerts.AsNoTracking().Where(a => a.UserId == userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var filtro = ParseList<AlertStatus>(status, "status");
                query = query.Where(a => filtro.Contains(a.Status));
            }
            else
            {
                query = query.Where(a => StatusPadrao.Contains(a.Status));
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                var filtro = ParseList<AlertType>(tipo, "tipo");
                query = query.Where(a => filtro.Contains(a.Type));
            }

            var items = await query.OrderByDescending(a => a.CreatedDate).Take(200).ToListAsync();
            var dependencyIds = items
                .Where(a => a.EntityType == NotificationRulesService.EntidadeDependencia && a.EntityId.HasValue)
                .Select(a => a.EntityId!.Value)
                .ToList();
            var origem = await _db.ProjectDependencies.AsNoTracking()
                .Where(d => dependencyIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.ProjectId);

            return items.Select(a =>
            {
                var dto = Map(a);
                dto.ProjetoId = a.EntityType switch
                {
                    NotificationRulesService.EntidadeProjeto => a.EntityId,
                    NotificationRulesService.EntidadeDependencia when a.EntityId.HasValue && origem.TryGetValue(a.EntityId.Value, out var p) => p,
                    _ => null,
                };
                return dto;
            });
        }

        public async Task<int> CountUnreadAsync()
        {
            var userId = _currentUser.RequireUserId();
            return await _db.Alerts.CountAsync(a => a.UserId == userId && a.Status == AlertStatus.Enviada);
        }

        public async Task<NotificacaoDto> MarkAsReadAsync(Guid id)
        {
            var alert = await FindMineAsync(id);
            if (alert.Status != AlertStatus.Lida)
            {
                alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.MarcarLida);
                alert.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Map(alert);
        }

        public async Task<NotificacaoDto> ArchiveAsync(Guid id)
        {
            var alert = await FindMineAsync(id);
            alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Arquivar);
            alert.ArchivedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Map(alert);
        }

        public async Task DispatchAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerts.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (alert == null)
            {
                return;
            }

            if (alert.Status == AlertStatus.Gerada)
            {
                alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Enfileirar);
            }

            if (AlertStateMachine.Permite(alert.Status, AlertStateMachine.Enviar))
            {
                // Envio por e-mail fica fora do escopo: a notificação é entregue na central (RF46).
                alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Enviar);
                alert.SentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ExpireAsync(DateTime agora, CancellationToken cancellationToken = default)
        {
            var limiteIgnorar = agora.AddDays(-DiasParaIgnorar);
            var ignorar = await _db.Alerts
                .Where(a => a.Status == AlertStatus.Enviada && a.SentAt != null && a.SentAt < limiteIgnorar)
                .ToListAsync(cancellationToken);
            foreach (var alert in ignorar)
            {
                alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Ignorar);
            }

            // Lida: 30 dias depois da leitura; Ignorada: 30 dias depois do envio sem leitura (7 + 23).
            var limiteArquivar = agora.AddDays(-DiasParaArquivar);
            var arquivar = await _db.Alerts
                .Where(a => (a.Status == AlertStatus.Lida && a.ReadAt != null && a.ReadAt < limiteArquivar) ||
                            (a.Status == AlertStatus.Ignorada && a.SentAt != null && a.SentAt < limiteArquivar))
                .ToListAsync(cancellationToken);
            foreach (var alert in arquivar)
            {
                alert.Status = AlertStateMachine.Aplicar(alert.Status, AlertStateMachine.Arquivar);
                alert.ArchivedAt = agora;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<Alert> FindMineAsync(Guid id)
        {
            var userId = _currentUser.RequireUserId();
            return await _db.Alerts.SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId)
                ?? throw new BusinessRuleException(404, "Notificação não encontrada.");
        }

        private static List<T> ParseList<T>(string value, string campo) where T : struct, Enum
        {
            var result = new List<T>();
            foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<T>(item, true, out var parsed) || !Enum.IsDefined(parsed))
                {
                    throw new BusinessRuleException(400, $"Valor inválido para {campo}: {item}.");
                }

                result.Add(parsed);
            }

            return result;
        }

        private static NotificacaoDto Map(Alert a) => new()
        {
            Id = a.Id,
            Tipo = a.Type.ToString(),
            Mensagem = a.Message,
            EntidadeTipo = a.EntityType,
            EntidadeId = a.EntityId,
            Status = a.Status.ToString(),
            DataCriacao = a.CreatedDate,
            DataEnvio = a.SentAt,
            DataLeitura = a.ReadAt,
            DataArquivamento = a.ArchivedAt,
        };
    }
}
