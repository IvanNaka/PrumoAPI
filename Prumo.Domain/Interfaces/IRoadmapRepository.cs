using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IRoadmapRepository : IRepository<RoadmapItem>
    {
        new Task<RoadmapItem?> GetByIdAsync(Guid id);
        Task<IEnumerable<RoadmapItem>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<int> GetNextOrderAsync(Guid portfolioId);
        Task UpdateRangeAsync(IEnumerable<RoadmapItem> items);
    }
}
