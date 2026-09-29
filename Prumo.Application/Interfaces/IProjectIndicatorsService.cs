using Prumo.Application.DTOs.Project;

namespace Prumo.Application.Interfaces
{
    public interface IProjectIndicatorsService
    {
        Task<ProjetoIndicadoresDto> GetAsync(Guid projectId);
    }
}
