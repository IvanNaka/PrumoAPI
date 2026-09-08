using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    public class Team : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short, unique, human-shareable code used by other users to join this team
        /// via <c>POST /api/teams/join</c> without needing an explicit invite from the owner.
        /// </summary>
        public string InviteCode { get; set; } = string.Empty;

        public Guid? OwnerUserId { get; set; }
        public User OwnerUser { get; set; }

        public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();
    }
}
