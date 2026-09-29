using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Portfolio relation
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; } = ProjectStatus.Rascunho;
        public Guid OwnerId { get; set; }
        public User Owner { get; set; }

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


        // Optional navigations
        public ICollection<ProjectObjective> ProjectObjectives { get; set; } = new List<ProjectObjective>();
        public ICollection<ProjectEvaluation> ProjectEvaluations { get; set; } = new List<ProjectEvaluation>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
        public ICollection<ProjectDependency> Dependencies { get; set; } = new List<ProjectDependency>();
        public ICollection<ProjectDependency> DependentProjects { get; set; } = new List<ProjectDependency>();
        public Budget Budget { get; set; }
    }
}
