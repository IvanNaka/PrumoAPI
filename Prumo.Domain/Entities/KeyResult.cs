namespace Prumo.Domain.Entities
{
    // KeyResult (Seção 3.2, RF15).
    public class KeyResult : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ObjectiveId { get; set; }
        public Objective Objective { get; set; } = null!;

        /// <summary>Descricao do Key Result.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Meta: maior que 0.</summary>
        public decimal TargetValue { get; set; }

        /// <summary>ValorAtual: padrão 0; maior ou igual a 0.</summary>
        public decimal CurrentValue { get; set; }
    }
}
