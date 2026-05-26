using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Application.DTOs.PriorityCriteria;

namespace Prumo.Application.Interfaces
{
    public interface IPriorityCriteriaService
    {
        Task<PriorityCriteriaDto> GetByIdAsync(Guid id);
        Task<IEnumerable<PriorityCriteriaDto>> GetAllAsync();
        Task<IEnumerable<PriorityCriteriaDto>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<IEnumerable<PriorityCriteriaDto>> GetByUserIdAsync(Guid userId);
        Task<PriorityCriteriaDto> CreateAsync(CreatePriorityCriteriaDto dto);
        Task UpdateAsync(UpdatePriorityCriteriaDto dto);
        Task DeleteAsync(Guid id);
    }
}