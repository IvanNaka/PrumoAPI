using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IEnumerable<Project>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId);
    }
}
