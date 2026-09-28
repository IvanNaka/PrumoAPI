using System;
using System.Collections.Generic;

namespace Prumo.Domain.Entities
{
    // 1:1 com Project (RF22) — orçamento aprovado do projeto, usado para
    // calcular Burn Rate (RF24) e VPL (RF25) a partir dos custos/despesas
    // registrados (RF23).
    public class Budget : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DiscountRateMonthly { get; set; }
        public decimal ExpectedReturn { get; set; }

        public ICollection<BudgetExpense> Expenses { get; set; } = new List<BudgetExpense>();
    }
}
