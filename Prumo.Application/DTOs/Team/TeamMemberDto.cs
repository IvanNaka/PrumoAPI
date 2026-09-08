using System;

namespace Prumo.Application.DTOs.Team
{
    public class TeamMemberDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
    }
}
