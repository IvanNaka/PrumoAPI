using System;

namespace Prumo.Application.DTOs.Team
{
    public class UpdateTeamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? OwnerUserId { get; set; }
    }
}
