using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prumo.Application.DTOs.Criteria;
using Prumo.Application.DTOs.PriorityCriteria;
using Prumo.Application.Interfaces;
using Prumo.Application.StateMachines;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class PriorityCriteriaService : IPriorityCriteriaService
    {
        private readonly IPriorityCriteriaRepository _priorityCriteriaRepository;
        private readonly IPortfolioService _portfolioService;
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;

        public PriorityCriteriaService(
            IPriorityCriteriaRepository priorityCriteriaRepository,
            IPortfolioService portfolioService,
            IAppDbContext db,
            IPortfolioAccessService access)
        {
            _priorityCriteriaRepository = priorityCriteriaRepository;
            _portfolioService = portfolioService;
            _db = db;
            _access = access;
        }

        /// <summary>GET /portfolios/{id}/criterios — exige ser membro do portfólio.</summary>
        public async Task<IEnumerable<CriterioDto>> ListByPortfolioAsync(Guid portfolioId)
        {
            await _access.EnsureAccessAsync(portfolioId);
            return await _db.PriorityCriterias.AsNoTracking()
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.Name)
                .Select(c => new CriterioDto { Id = c.Id, PortfolioId = c.PortfolioId, Nome = c.Name, Peso = c.ValueWeight })
                .ToListAsync();
        }

        public async Task<PriorityCriteriaDto> GetByIdAsync(Guid id)
        {
            var criteria = await _priorityCriteriaRepository.GetByIdAsync(id);
            if (criteria == null) return null;

            return MapToDto(criteria);
        }

        public async Task<IEnumerable<PriorityCriteriaDto>> GetAllAsync()
        {
            var criteriaList = await _priorityCriteriaRepository.GetAllAsync();
            return criteriaList.Select(MapToDto);
        }

        public async Task<IEnumerable<PriorityCriteriaDto>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            var criteriaList = await _priorityCriteriaRepository.GetByPortfolioIdAsync(portfolioId);
            return criteriaList.Select(MapToDto);
        }

        public async Task<IEnumerable<PriorityCriteriaDto>> GetByUserIdAsync(Guid userId)
        {
            var criteriaList = await _priorityCriteriaRepository.GetByUserIdAsync(userId);
            return criteriaList.Select(MapToDto);
        }

        public async Task<PriorityCriteriaDto> CreateAsync(CreatePriorityCriteriaDto dto)
        {
            var criteria = new PriorityCriteria
            {
                Name = dto.Name,
                ValueWeight = dto.ValueWeight,
                PortfolioId = dto.PortfolioId,
                UserId = dto.UserId
            };

            await _priorityCriteriaRepository.AddAsync(criteria);

            // Figura 27: Criado -> Configurado no primeiro critério; Monitoramento -> Reavaliacao.
            await _portfolioService.ApplyAutomaticEventAsync(criteria.PortfolioId, PortfolioStateMachine.PrimeiroCriterio);
            await _portfolioService.ApplyAutomaticEventAsync(criteria.PortfolioId, PortfolioStateMachine.AlterarCriterio);

            return MapToDto(criteria);
        }

        public async Task UpdateAsync(UpdatePriorityCriteriaDto dto)
        {
            var existingCriteria = await _priorityCriteriaRepository.GetByIdAsync(dto.Id);
            if (existingCriteria != null)
            {
                existingCriteria.Name = dto.Name;
                existingCriteria.ValueWeight = dto.ValueWeight;

                await _priorityCriteriaRepository.UpdateAsync(existingCriteria);
                await _portfolioService.ApplyAutomaticEventAsync(existingCriteria.PortfolioId, PortfolioStateMachine.AlterarCriterio);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var existing = await _priorityCriteriaRepository.GetByIdAsync(id);
            await _priorityCriteriaRepository.DeleteAsync(id);
            if (existing != null)
            {
                await _portfolioService.ApplyAutomaticEventAsync(existing.PortfolioId, PortfolioStateMachine.AlterarCriterio);
            }
        }

        private static PriorityCriteriaDto MapToDto(PriorityCriteria criteria)
        {
            return new PriorityCriteriaDto
            {
                Id = criteria.Id,
                Name = criteria.Name,
                ValueWeight = criteria.ValueWeight,
                PortfolioId = criteria.PortfolioId,
                UserId = criteria.UserId
            };
        }
    }
}