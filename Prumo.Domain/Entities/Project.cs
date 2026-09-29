using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    // Projeto (Seção 3.2, RF10–RF13, RF22).
    public class Project : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Todo projeto pertence a um portfólio (RF12).</summary>
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Responsável (ResponsavelId).</summary>
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        /// <summary>DataInicio.</summary>
        public DateOnly StartDate { get; set; }

        /// <summary>DataFim: maior ou igual a DataInicio (RN12).</summary>
        public DateOnly EndDate { get; set; }

        /// <summary>OrcamentoAprovado (RF22): maior ou igual a 0.</summary>
        public decimal ApprovedBudget { get; set; }

        /// <summary>CategoriaEstrategica (Run/Grow/Transform).</summary>
        public StrategicCategory StrategicCategory { get; set; }

        /// <summary>Figura 26 + D04. Padrão: Rascunho.</summary>
        public ProjectStatus Status { get; set; } = ProjectStatus.Rascunho;

        /// <summary>Prioridade (padrão Media); desempate do ranking (F2).</summary>
        public Priority Priority { get; set; } = Priority.Media;

        /// <summary>StatusAvaliacao (Figura 28). Padrão NaoAvaliado.</summary>
        public EvaluationStatus EvaluationStatus { get; set; } = EvaluationStatus.NaoAvaliado;

        /// <summary>ScoreAtual (0 a 100); nulo enquanto não estiver priorizado.</summary>
        public decimal? CurrentScore { get; set; }

        /// <summary>PosicaoRanking.</summary>
        public int? RankingPosition { get; set; }

        /// <summary>DataUltimaPriorizacao.</summary>
        public DateTime? LastPrioritizationDate { get; set; }

        /// <summary>Chave do projeto no Jira, usada na sincronização.</summary>
        public string? JiraProjectKey { get; set; }

        /// <summary>DataConclusao: preenchida ao passar para Concluido.</summary>
        public DateTime? CompletedAt { get; set; }

        // Navegações
        public ICollection<ProjectObjective> ProjectObjectives { get; set; } = new List<ProjectObjective>();
        public ICollection<ProjectEvaluation> ProjectEvaluations { get; set; } = new List<ProjectEvaluation>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public ICollection<ProjectDependency> Dependencies { get; set; } = new List<ProjectDependency>();
        public ICollection<ProjectDependency> DependentProjects { get; set; } = new List<ProjectDependency>();
        public Budget? Budget { get; set; }
        public ICollection<BudgetExpense> Expenses { get; set; } = new List<BudgetExpense>();
        public BusinessCase? BusinessCase { get; set; }
        public ICollection<RealizedReturn> RealizedReturns { get; set; } = new List<RealizedReturn>();
    }
}
