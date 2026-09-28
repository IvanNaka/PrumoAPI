using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Application.DTOs.Roadmap;

namespace Prumo.Application.Interfaces
{
    public interface IRoadmapService
    {
        Task<IEnumerable<RoadmapItemDto>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<RoadmapItemDto> GetByIdAsync(Guid id);
        Task<RoadmapItemDto> CreateAsync(CreateRoadmapItemDto dto);
        Task UpdateAsync(UpdateRoadmapItemDto dto);
        Task DeleteAsync(Guid id);
        Task ReorderAsync(Guid portfolioId, ReorderRoadmapDto dto);
    }
}
