using System;
using Prumo.Domain.Enums;

namespace Prumo.Application.DTOs.BudgetExpense
{
    public class CreateBudgetExpenseDto
    {
        public string Description { get; set; } = string.Empty;
        public BudgetExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
