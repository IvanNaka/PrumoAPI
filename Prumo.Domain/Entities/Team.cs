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

        public Guid? OwnerUserId { get; set; }
        public User OwnerUser { get; set; }

        public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();
    }
}
