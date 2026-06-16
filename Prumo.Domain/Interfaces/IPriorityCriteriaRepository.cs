using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IPriorityCriteriaRepository : IRepository<PriorityCriteria>
    {
        Task<IEnumerable<PriorityCriteria>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<IEnumerable<PriorityCriteria>> GetByUserIdAsync(Guid userId);
    }
}