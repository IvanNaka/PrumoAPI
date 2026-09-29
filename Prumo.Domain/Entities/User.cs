using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    // Usuario (Seção 3.2). O login é exclusivamente pelo Google (D01): não há senha.
    public class User : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        /// <summary>Sempre gravado em minúsculas; único.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Ativo: somente usuários ativos conseguem entrar (RN03).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Perfis do usuário (D03: um usuário pode ter vários perfis).</summary>
        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

        // Navigation properties
        public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<TeamUser> TeamMemberships { get; set; } = new List<TeamUser>();
    }
}
