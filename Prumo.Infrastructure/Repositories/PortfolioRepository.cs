using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class PortfolioRepository : Repository<Portfolio>, IPortfolioRepository
    {
        public PortfolioRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Portfolio>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _dbSet
                .Where(p => p.OwnerId == ownerId)
                .Include(p => p.Owner)
                .ToListAsync();
        }

        public async Task<Portfolio?> GetPortfolioWithProjectsAsync(Guid portfolioId)
        {
            return await _dbSet
                .Include(p => p.Projects)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == portfolioId);
        }
    }
}