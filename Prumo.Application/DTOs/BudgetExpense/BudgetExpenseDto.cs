using System;
using Prumo.Domain.Enums;

namespace Prumo.Application.DTOs.BudgetExpense
{
    public class BudgetExpenseDto
    {
        public Guid Id { get; set; }
        public Guid BudgetId { get; set; }
        public string Description { get; set; } = string.Empty;
        public BudgetExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
