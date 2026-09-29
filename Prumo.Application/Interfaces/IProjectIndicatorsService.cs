using Prumo.Application.DTOs.Project;

namespace Prumo.Application.Interfaces
{
    public interface IProjectIndicatorsService
    {
        /// <summary>F5, F6, F7, F8 e F12 do projeto; F7 e F8 no período (padrão: últimos 90 dias).</summary>
        Task<ProjetoIndicadoresDto> GetAsync(Guid projectId, DateOnly? de = null, DateOnly? ate = null);
    }
}
