using Prumo.Application.DTOs.Budget;
using Prumo.Application.DTOs.BudgetExpense;

namespace Prumo.Application.Interfaces
{
    public interface IBudgetService
    {
        Task<BudgetDto> GetByIdAsync(Guid id);
        Task<BudgetDto> GetByProjectIdAsync(Guid projectId);
        Task<BudgetDto> CreateAsync(CreateBudgetDto dto);
        Task UpdateAsync(Guid id, UpdateBudgetDto dto);

        Task<IEnumerable<BudgetExpenseDto>> GetExpensesAsync(Guid budgetId);
        Task<BudgetExpenseDto> AddExpenseAsync(Guid budgetId, CreateBudgetExpenseDto dto);
        Task<BudgetExpenseDto> GetExpenseByIdAsync(Guid expenseId);
        Task DeleteExpenseAsync(Guid expenseId);

        Task<BudgetMetricsDto> GetMetricsAsync(Guid budgetId);
    }
}
