using System;
using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // N:1 com Budget (RF23) — custo/despesa individual registrado contra um
    // orçamento, usado no cálculo de Burn Rate/VPL.
    public class BudgetExpense : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BudgetId { get; set; }
        public Budget Budget { get; set; } = null!;

        public string Description { get; set; } = string.Empty;
        public BudgetExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
