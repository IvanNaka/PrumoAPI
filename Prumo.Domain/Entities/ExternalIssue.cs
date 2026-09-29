using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // Issue (dados vindos do Jira, RF47). Upsert por IdExterno.
    public class ExternalIssue
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>IdExterno: chave do Jira (ex.: PRU-12); único.</summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>Origem do dado: "Jira" (ou "Seed" nos dados de exemplo).</summary>
        public string Source { get; set; } = "Jira";

        public string Title { get; set; } = string.Empty;
        public ExternalIssueType Type { get; set; }

        /// <summary>Nome do status no Jira.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Concluida: statusCategory.key == "done".</summary>
        public bool Done { get; set; }

        public decimal? EstimateHours { get; set; }

        /// <summary>HorasRealizadas: soma dos worklogs.</summary>
        public decimal SpentHours { get; set; }

        /// <summary>ResponsavelEmail (minúsculas).</summary>
        public string? AssigneeEmail { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateOnly? DueDate { get; set; }

        /// <summary>UltimaAtualizacao: campo "updated" do Jira.</summary>
        public DateTime UpdatedAt { get; set; }

        public ICollection<ExternalWorklog> Worklogs { get; set; } = new List<ExternalWorklog>();
    }

    // Worklog (RF47). Upsert por IdExterno.
    public class ExternalWorklog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid IssueId { get; set; }
        public ExternalIssue Issue { get; set; } = null!;

        /// <summary>IdExterno: id do worklog no Jira; único.</summary>
        public string ExternalId { get; set; } = string.Empty;

        /// <summary>AutorEmail (minúsculas).</summary>
        public string? AuthorEmail { get; set; }

        /// <summary>Horas: segundos do Jira ÷ 3600.</summary>
        public decimal Hours { get; set; }

        public DateOnly Date { get; set; }
    }
}
