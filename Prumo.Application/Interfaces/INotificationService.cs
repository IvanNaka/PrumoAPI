using Prumo.Application.DTOs.Notification;
using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>NotificacaoService (RF45, RF46, Figura 30).</summary>
    public interface INotificationService
    {
        /// <summary>
        /// Cria a notificação (Gerada → Enfileirada), exceto se já houver outra com o mesmo
        /// (usuário, tipo, entidadeId) fora de Arquivada nas últimas 24 h. Devolve false quando deduplicada.
        /// </summary>
        Task<bool> CreateAsync(Guid userId, AlertType type, string message, string? entityType, Guid? entityId, CancellationToken cancellationToken = default);

        /// <summary>Notificações do usuário logado (padrão: Enviada, Lida e Ignorada).</summary>
        Task<IEnumerable<NotificacaoDto>> ListMineAsync(string? status, string? tipo);

        Task<int> CountUnreadAsync();

        Task<NotificacaoDto> MarkAsReadAsync(Guid id);

        Task<NotificacaoDto> ArchiveAsync(Guid id);

        /// <summary>Dispatcher: Enfileirada → Enviada (DataEnvio).</summary>
        Task DispatchAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Job diário: Enviada há mais de 7 dias → Ignorada; Lida/Ignorada há mais de 30 dias → Arquivada.</summary>
        Task ExpireAsync(DateTime agora, CancellationToken cancellationToken = default);
    }

    /// <summary>Fila do dispatcher de notificações.</summary>
    public interface INotificationQueue
    {
        void Enqueue(Guid notificationId);
    }

    /// <summary>RegrasNotificacaoService: aplica as regras da T21 (job diário e depois de cada sincronização).</summary>
    public interface INotificationRulesService
    {
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
