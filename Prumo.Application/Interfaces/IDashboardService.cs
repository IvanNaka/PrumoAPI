using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetAsync(Guid portfolioId, DateOnly? de, DateOnly? ate);

        /// <summary>Um indicador só (nome = uma das 8 chaves do dashboard).</summary>
        Task<IndicatorResult> GetIndicatorAsync(Guid portfolioId, string nome, DateOnly? de, DateOnly? ate);
    }
}
