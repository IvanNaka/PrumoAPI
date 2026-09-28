using System;

namespace Prumo.Application.DTOs.TeamCapacity
{
    public class CreateTeamCapacityDto
    {
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal AvailableHours { get; set; }
    }
}
