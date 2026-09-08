using System;
using System.Collections.Generic;

namespace Prumo.Application.DTOs.Team
{
    public class TeamDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string? PortfolioName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public Guid? OwnerUserId { get; set; }
        public string? OwnerUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<TeamMemberDto> Members { get; set; } = new List<TeamMemberDto>();
    }
}
