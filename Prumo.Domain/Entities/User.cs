using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public Guid RoleId { get; set; }
        public Role Role { get; set; }

        // Navigation properties
        public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<TeamUser> TeamMemberships { get; set; } = new List<TeamUser>();
    }
}
