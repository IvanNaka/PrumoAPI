using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Project>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            return await _dbSet
                .Where(p => p.PortfolioId == portfolioId)
                .Include(p => p.Owner)
                .Include(p => p.Portfolio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _dbSet
                .Where(p => p.OwnerId == ownerId)
                .Include(p => p.ProjectEvaluations)
                .Include(p => p.Owner)
                .Include(p => p.Portfolio)
                .ToListAsync();
        }
        public async Task<Project?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(p=>p.Owner)
                .Include(p=>p.Portfolio)
                .Include(p=>p.ProjectEvaluations)
                .Include(p=>p.Portfolio.PriorityCriterias)
                .FirstOrDefaultAsync(p=> p.Id == id);
        }
    }
}
