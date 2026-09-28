namespace Prumo.Domain.Interfaces
{
    using Prumo.Domain.Entities;

    public interface IBudgetExpenseRepository : IRepository<BudgetExpense>
    {
        Task<IEnumerable<BudgetExpense>> GetByBudgetIdAsync(Guid budgetId);
    }
}
