using Prumo.Application.DTOs.ProjectDependency;
using Prumo.Application.Interfaces;

namespace Prumo.Application.Interfaces
{
    public interface IProjectDependencyService
    {
        Task<ProjectDependencyDto> GetByIdAsync(Guid id);
        Task<IEnumerable<ProjectDependencyDto>> GetAllAsync();
        Task<IEnumerable<ProjectDependencyDto>> GetAllByPortfolioAsync(Guid portfolioId);
        Task<ProjectDependencyDto> CreateAsync(CreateProjectDependencyDto dto);
        Task DeleteAsync(Guid id);
    }
}