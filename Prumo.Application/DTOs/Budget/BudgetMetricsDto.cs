using System;

namespace Prumo.Application.DTOs.Budget
{
    public class BudgetMetricsDto
    {
        public Guid BudgetId { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal RemainingBudget { get; set; }
        public int PercentSpent { get; set; }
        public decimal NetPresentValue { get; set; }
        public decimal BurnRateMonthly { get; set; }
        public int BurnRatePercent { get; set; }
        public int MonthsElapsed { get; set; }
        public int MonthsRemaining { get; set; }
        public DateTime? ProjectedDepletionDate { get; set; }
        public bool IsOverBudgetRisk { get; set; }
    }
}
