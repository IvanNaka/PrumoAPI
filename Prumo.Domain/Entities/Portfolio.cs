using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    public class Portfolio : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid OwnerId { get; set; }
        public User Owner { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<PriorityCriteria> PriorityCriterias { get; set; } = new List<PriorityCriteria>();
        public ICollection<ProjectDependency> ProjectDependencies { get; set; } = new List<ProjectDependency>();
    }
}