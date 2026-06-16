using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class PriorityCriteriaRepository : Repository<PriorityCriteria>, IPriorityCriteriaRepository
    {
        public PriorityCriteriaRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PriorityCriteria>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            return await _dbSet.Where(pc => pc.PortfolioId == portfolioId).ToListAsync();
        }

        public async Task<IEnumerable<PriorityCriteria>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet.Where(pc => pc.UserId == userId).ToListAsync();
        }
    }
}