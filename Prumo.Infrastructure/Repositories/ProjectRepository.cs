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
            return await _dbSet.Where(p => p.PortfolioId == portfolioId).ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _dbSet.Where(p => p.OwnerId == ownerId).ToListAsync();
        }
    }
}
