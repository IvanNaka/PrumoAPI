using Prumo.Application.DTOs.Criteria;

namespace Prumo.Application.Interfaces
{
    // UC4–UC6, RF07–RF09.
    public interface IPriorityCriteriaService
    {
        Task<IEnumerable<CriterioDto>> ListByPortfolioAsync(Guid portfolioId);
        Task<CriterioDto> CreateAsync(Guid portfolioId, SalvarCriterioDto dto);
        Task<CriterioDto> UpdateAsync(Guid id, SalvarCriterioDto dto);
        Task DeleteAsync(Guid id);
    }
}
