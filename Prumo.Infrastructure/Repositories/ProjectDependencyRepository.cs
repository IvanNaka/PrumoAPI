using Microsoft.EntityFrameworkCore;
using Plantonize.Plantao.Infrastructure;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class ProjectDependencyRepository : Repository<ProjectDependency>, IProjectDependencyRepository
    {
        public ProjectDependencyRepository(PrumoDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<ProjectDependency>> GetAllByPortfolioAsync(Guid portfolioId)
        {
            return await _dbSet
                .Where(p => p.PortfolioId == portfolioId)
                .Include(p => p.User)
                .Include(p => p.DependsOnProject)
                .Include(p => p.Project)
                .Include(p => p.Portfolio)
                .ToListAsync();
        }
    }
}