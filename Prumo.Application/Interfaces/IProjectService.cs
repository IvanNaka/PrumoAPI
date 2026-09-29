using Prumo.Application.DTOs.Project;

namespace Prumo.Application.Interfaces
{
    // RF10–RF13, UC7, Figura 26.
    public interface IProjectService
    {
        Task<IEnumerable<ProjetoResumoDto>> ListByPortfolioAsync(Guid portfolioId, string? status = null, string? categoria = null);
        Task<ProjetoDetalheDto> GetDetailAsync(Guid id);
        Task<ProjetoDetalheDto> CreateAsync(Guid portfolioId, SalvarProjetoDto dto);
        Task<ProjetoDetalheDto> UpdateAsync(Guid id, SalvarProjetoDto dto);
        Task<ProjetoDetalheDto> ChangeStatusAsync(Guid id, string acao);
    }
}
