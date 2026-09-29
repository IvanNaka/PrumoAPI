using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // LancamentoFinanceiro (RF23): custo ou despesa registrado no projeto.
    public class BudgetExpense : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        /// <summary>Tipo: Custo ou Despesa.</summary>
        public BudgetExpenseCategory Category { get; set; }

        /// <summary>Valor: maior que 0.</summary>
        public decimal Amount { get; set; }

        /// <summary>DataLancamento: não pode ser no futuro.</summary>
        public DateOnly Date { get; set; }
    }
}
