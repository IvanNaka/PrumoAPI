using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class BudgetRepository : Repository<Budget>, IBudgetRepository
    {
        public BudgetRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<Budget?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Budget?> GetByProjectIdAsync(Guid projectId)
        {
            return await _dbSet
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.ProjectId == projectId);
        }

        public async Task<Budget?> GetByIdWithExpensesAsync(Guid id)
        {
            return await _dbSet
                .Include(b => b.Project)
                .Include(b => b.Expenses)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
    }
}
