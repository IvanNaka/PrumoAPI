using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepository;

        public PortfolioService(IPortfolioRepository portfolioRepository)
        {
            _portfolioRepository = portfolioRepository;
        }

        public async Task<PortfolioDto> GetByIdAsync(Guid id)
        {
            var portfolio = await _portfolioRepository.GetByIdAsync(id);
            if (portfolio == null) return null;

            return MapToDto(portfolio);
        }

        public async Task<IEnumerable<PortfolioDto>> GetAllAsync()
        {
            var portfolios = await _portfolioRepository.GetAllAsync();
            return portfolios.Select(MapToDto);
        }

        public async Task<IEnumerable<PortfolioDto>> GetByOwnerIdAsync(Guid ownerId)
        {
            var portfolios = await _portfolioRepository.GetByOwnerIdAsync(ownerId);
            return portfolios.Select(MapToDto);
        }

        public async Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto)
        {
            var portfolio = new Portfolio
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = dto.OwnerId,
            };

            await _portfolioRepository.AddAsync(portfolio);

            return MapToDto(portfolio);
        }

        public async Task UpdateAsync(UpdatePortfolioDto dto)
        {
            var existingPortfolio = await _portfolioRepository.GetByIdAsync(dto.Id);
            if (existingPortfolio != null)
            {
                existingPortfolio.Name = dto.Name;
                existingPortfolio.Description = dto.Description;

                await _portfolioRepository.UpdateAsync(existingPortfolio);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            await _portfolioRepository.DeleteAsync(id);
        }

        private static PortfolioDto MapToDto(Portfolio portfolio)
        {
            return new PortfolioDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                Description = portfolio.Description,
                OwnerId = portfolio.OwnerId,
                OwnerName = portfolio.Owner?.Name ?? string.Empty,
                CreatedAt = portfolio.CreatedDate
            };
        }
    }
}