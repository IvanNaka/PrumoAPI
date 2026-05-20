using System;

namespace Prumo.Domain.Entities
{
    public class ProjectMember : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
