using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IPortfolioRepository : IRepository<Portfolio>
    {
        Task<IEnumerable<Portfolio>> GetByOwnerIdAsync(Guid ownerId);
        Task<Portfolio?> GetPortfolioWithProjectsAsync(Guid portfolioId);
    }
}