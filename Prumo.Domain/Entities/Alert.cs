using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // Notificacao (RF45, RF46, Figura 30). CreatedDate (BaseEntity) = DataCriacao.
    public class Alert : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>UsuarioId: destinatário.</summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public AlertType Type { get; set; }

        /// <summary>Mensagem (até 500 caracteres).</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>EntidadeTipo: "Projeto", "Equipe" ou "Dependencia".</summary>
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }

        /// <summary>Status: padrão Gerada (D12).</summary>
        public AlertStatus Status { get; set; } = AlertStatus.Gerada;

        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
    }
}
