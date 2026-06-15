using System;

namespace Prumo.Domain.Entities
{
    public class ProjectDependency : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Reason { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid DependsOnProjectId { get; set; }
        public Project DependsOnProject { get; set; } = null!;
    }
}