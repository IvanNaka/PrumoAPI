using System;

namespace Prumo.Domain.Entities
{
    // N:1 com Team, N:1 com User — capacidade/ocupação mensal de um
    // colaborador dentro de uma equipe (US13). Único por (TeamId, UserId,
    // Year, Month).
    public class TeamCapacityEntry : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public int Year { get; set; }
        public int Month { get; set; }

        public decimal AvailableHours { get; set; }
        public decimal AllocatedHours { get; set; }
        public int OccupancyPercent { get; set; }
    }
}
