namespace Prumo.Domain.Entities
{
    // Orcamento (Figura 9) — 1:1 com Projeto, com TotalAmount (ValorPlanejado) sempre igual a
    // Project.ApprovedBudget. O valor consumido e o burn rate NÃO são gravados: são calculados (F5).
    public class Budget : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>ValorPlanejado == Project.ApprovedBudget.</summary>
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "BRL";
    }
}
