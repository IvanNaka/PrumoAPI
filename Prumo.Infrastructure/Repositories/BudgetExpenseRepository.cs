using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class BudgetExpenseRepository : Repository<BudgetExpense>, IBudgetExpenseRepository
    {
        public BudgetExpenseRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BudgetExpense>> GetByBudgetIdAsync(Guid budgetId)
        {
            return await _dbSet
                .Where(e => e.BudgetId == budgetId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
    }
}
