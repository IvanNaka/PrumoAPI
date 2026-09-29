using System;

namespace Prumo.Domain.Entities
{
    // MembroEquipe (RF30, RF31). O colaborador pode não usar o sistema (UserId opcional); o e-mail
    // liga o membro ao responsável/autor dos worklogs no Jira.
    public class TeamUser : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Sempre em minúsculas.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>CustoHora: maior ou igual a 0.</summary>
        public decimal HourlyCost { get; set; }

        /// <summary>CapacidadeMensalHoras: de 1 a 300.</summary>
        public int MonthlyCapacityHours { get; set; }
    }
}
