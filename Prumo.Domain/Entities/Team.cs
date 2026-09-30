using System;
using System.Collections.Generic;
using System.Security.Cryptography;

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

        /// <summary>Código de convite: quem ainda não participa do Prumo entra na equipe informando-o.</summary>
        public string InviteCode { get; set; } = NewInviteCode();

        public ICollection<TeamUser> Members { get; set; } = new List<TeamUser>();

        // Sem caracteres ambíguos (0/O, 1/I/L).
        private const string InviteAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        public const int InviteCodeLength = 8;

        public static string NewInviteCode() =>
            RandomNumberGenerator.GetString(InviteAlphabet, InviteCodeLength);
    }
}
