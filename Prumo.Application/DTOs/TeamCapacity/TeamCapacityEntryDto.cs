using System;

namespace Prumo.Application.DTOs.TeamCapacity
{
    public class TeamCapacityEntryDto
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal AvailableHours { get; set; }
        public decimal AllocatedHours { get; set; }
        public int OccupancyPercent { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
