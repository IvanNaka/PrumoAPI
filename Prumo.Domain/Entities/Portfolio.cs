using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    // Portfolio (Seção 3.2, RF04, D10).
    public class Portfolio : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Obrigatório; único entre os portfólios não encerrados.</summary>
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Objetivo do portfólio (D10).</summary>
        public string? Goal { get; set; }

        /// <summary>Responsável (ResponsavelId).</summary>
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        /// <summary>Figura 27. Padrão: Criado.</summary>
        public PortfolioStatus Status { get; set; } = PortfolioStatus.Criado;

        /// <summary>Membros do portfólio (o responsável é incluído automaticamente).</summary>
        public ICollection<PortfolioMember> Members { get; set; } = new List<PortfolioMember>();

        /// <summary>OKRs associados ao portfólio (PortfolioOkr).</summary>
        public ICollection<PortfolioObjective> Objectives { get; set; } = new List<PortfolioObjective>();

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<PriorityCriteria> PriorityCriterias { get; set; } = new List<PriorityCriteria>();
        public ICollection<ProjectDependency> ProjectDependencies { get; set; } = new List<ProjectDependency>();
        public ICollection<RoadmapItem> RoadmapItems { get; set; } = new List<RoadmapItem>();
    }
}
