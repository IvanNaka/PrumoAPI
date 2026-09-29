using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    // Equipe (RF29).
    public class Team : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Opcional: equipe vinculada a um portfólio (usada pelo indicador de capacidade).</summary>
        public Guid? PortfolioId { get; set; }
        public Portfolio? Portfolio { get; set; }

        /// <summary>Nome único (100).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Usuário que cadastrou a equipe.</summary>
        public Guid? OwnerUserId { get; set; }
        public User? OwnerUser { get; set; }

        public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();
    }
}
