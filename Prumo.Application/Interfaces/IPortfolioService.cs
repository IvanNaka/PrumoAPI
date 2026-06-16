using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Application.DTOs.Portfolio;

namespace Prumo.Application.Interfaces
{
    public interface IPortfolioService
    {
        Task<PortfolioDto> GetByIdAsync(Guid id);
        Task<IEnumerable<PortfolioDto>> GetAllAsync();
        Task<IEnumerable<PortfolioDto>> GetByOwnerIdAsync(Guid ownerId);
        Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto);
        Task UpdateAsync(UpdatePortfolioDto dto);
        Task DeleteAsync(Guid id);
    }
}