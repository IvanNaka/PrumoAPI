using System;

namespace Prumo.Domain.Entities
{
    public class TeamUser : BaseEntity
    {
        public Guid TeamId { get; set; }
        public Team Team { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
