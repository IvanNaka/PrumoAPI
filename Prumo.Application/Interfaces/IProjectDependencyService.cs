using Prumo.Application.DTOs.Dependency;

namespace Prumo.Application.Interfaces
{
    // Dependências entre projetos (RF26–RF28, UC12).
    public interface IProjectDependencyService
    {
        Task<IEnumerable<DependenciaDto>> ListByPortfolioAsync(Guid portfolioId);
        Task<IEnumerable<DependenciaDto>> ListByProjectAsync(Guid projectId);
        Task<DependenciaDto> CreateAsync(CriarDependenciaDto dto);
        Task DeleteAsync(Guid id);
    }
}
