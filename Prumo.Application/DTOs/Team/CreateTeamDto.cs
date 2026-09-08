using System;

namespace Prumo.Application.DTOs.Team
{
    public class CreateTeamDto
    {
        public Guid PortfolioId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? OwnerUserId { get; set; }
    }
}
