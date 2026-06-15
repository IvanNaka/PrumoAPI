using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetByIdAsync(Guid id);
        Task<IEnumerable<Project>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<IEnumerable<Project>> GetByOwnerIdAsync(Guid ownerId);
    }
}
