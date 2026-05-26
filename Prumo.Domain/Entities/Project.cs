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
        public ProjectStatus Status { get; set; }
        public Guid OwnerId { get; set; }
        public User Owner { get; set; }


        // Optional navigations
        public ICollection<ProjectObjective> ProjectObjectives { get; set; } = new List<ProjectObjective>();
        public ICollection<ProjectEvaluation> ProjectEvaluations { get; set; } = new List<ProjectEvaluation>();
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
