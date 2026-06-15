namespace Prumo.Domain.Interfaces
{
    using Prumo.Domain.Entities;

    public interface IProjectDependencyRepository : IRepository<ProjectDependency>
    {
        Task<IEnumerable<ProjectDependency>> GetAllByPortfolioAsync(Guid portfolioId);
    }
}