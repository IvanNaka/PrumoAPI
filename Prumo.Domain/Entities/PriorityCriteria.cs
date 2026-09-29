using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // CriterioPrioridade (Seção 3.2, RF07, D06).
    public class PriorityCriteria : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Obrigatório; único dentro do portfólio.</summary>
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        /// <summary>Peso: maior que 0 e menor ou igual a 10.</summary>
        public decimal ValueWeight { get; set; }

        /// <summary>Beneficio (quanto maior, melhor) ou Custo (quanto menor, melhor).</summary>
        public CriteriaType Type { get; set; } = CriteriaType.Beneficio;

        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        /// <summary>Usuário que cadastrou o critério.</summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
