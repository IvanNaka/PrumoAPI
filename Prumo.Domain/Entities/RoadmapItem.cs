using System;
using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // N:1 com Portfolio, N:1 opcional com Project — fase/marco planejado do
    // roadmap de um portfólio (US09).
    public class RoadmapItem : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public RoadmapStatus Status { get; set; } = RoadmapStatus.Planejado;

        public int Order { get; set; }
    }
}
