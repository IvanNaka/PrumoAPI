using System;

namespace Prumo.Application.DTOs.Budget
{
    public class UpdateBudgetDto
    {
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DiscountRateMonthly { get; set; }
        public decimal ExpectedReturn { get; set; }
    }
}
