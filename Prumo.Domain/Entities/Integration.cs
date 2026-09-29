using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // IntegracaoConfig (RF47, RF51, Figura 29).
    public class Integration : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Provedor: Jira (futuramente AzureDevOps, GitHub, Trello — D09).</summary>
        public IntegrationType Type { get; set; } = IntegrationType.Jira;

        /// <summary>Url da instância (ex.: https://empresa.atlassian.net).</summary>
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>E-mail da conta usada no Jira.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>ApiTokenCriptografado (ASP.NET Data Protection). Nunca é devolvido pela API.</summary>
        public string Token { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>IntervaloSincronizacaoMinutos: de 15 a 1440; padrão 60 (RF51).</summary>
        public int SyncIntervalMinutes { get; set; } = 60;

        public IntegrationStatus Status { get; set; } = IntegrationStatus.NaoConfigurada;

        /// <summary>UltimaSincronizacao (bem-sucedida).</summary>
        public DateTime? LastSyncedAt { get; set; }

        public int FailedAttempts { get; set; }
        public DateTime? NextAttemptAt { get; set; }

        public ICollection<IntegrationSyncLog> Logs { get; set; } = new List<IntegrationSyncLog>();
    }

    // SincronizacaoLog: gravado em todas as execuções (sucesso ou erro).
    public class IntegrationSyncLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IntegrationId { get; set; }
        public Integration Integration { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public bool Success { get; set; }
        public int IssuesProcessed { get; set; }
        public int WorklogsProcessed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
