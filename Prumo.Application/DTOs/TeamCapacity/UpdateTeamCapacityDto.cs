using System;

namespace Prumo.Application.DTOs.TeamCapacity
{
    public class UpdateTeamCapacityDto
    {
        public Guid Id { get; set; }
        public decimal AvailableHours { get; set; }
    }
}
