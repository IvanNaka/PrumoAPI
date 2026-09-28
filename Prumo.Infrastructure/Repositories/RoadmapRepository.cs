using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class RoadmapRepository : Repository<RoadmapItem>, IRoadmapRepository
    {
        public RoadmapRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<RoadmapItem?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<RoadmapItem>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            return await _dbSet
                .Where(r => r.PortfolioId == portfolioId)
                .Include(r => r.Project)
                .OrderBy(r => r.Order)
                .ToListAsync();
        }

        public async Task<int> GetNextOrderAsync(Guid portfolioId)
        {
            var hasAny = await _dbSet.AnyAsync(r => r.PortfolioId == portfolioId);
            if (!hasAny)
            {
                return 1;
            }

            var maxOrder = await _dbSet
                .Where(r => r.PortfolioId == portfolioId)
                .MaxAsync(r => r.Order);

            return maxOrder + 1;
        }

        public async Task UpdateRangeAsync(IEnumerable<RoadmapItem> items)
        {
            _dbSet.UpdateRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
