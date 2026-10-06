using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prumo.Application.DTOs.Roadmap;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class RoadmapService : IRoadmapService
    {
        private readonly IRoadmapRepository _roadmapRepository;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IProjectRepository _projectRepository;

        public RoadmapService(
            IRoadmapRepository roadmapRepository,
            IPortfolioRepository portfolioRepository,
            IProjectRepository projectRepository)
        {
            _roadmapRepository = roadmapRepository;
            _portfolioRepository = portfolioRepository;
            _projectRepository = projectRepository;
        }

        public async Task<IEnumerable<RoadmapItemDto>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            var items = await _roadmapRepository.GetByPortfolioIdAsync(portfolioId);
            return items.Select(MapToDto);
        }

        public async Task<RoadmapItemDto> GetByIdAsync(Guid id)
        {
            var item = await _roadmapRepository.GetByIdAsync(id);
            return item == null ? null : MapToDto(item);
        }

        public async Task<RoadmapItemDto> CreateAsync(CreateRoadmapItemDto dto)
        {
            ValidateTitle(dto.Title);
            ValidateDates(dto.StartDate, dto.EndDate);

            var portfolioExists = await _portfolioRepository.ExistsAsync(dto.PortfolioId);
            if (!portfolioExists)
            {
                throw new ArgumentException("portfolioId inexistente.");
            }

            if (dto.ProjectId.HasValue)
            {
                var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId.Value);
                if (!projectExists)
                {
                    throw new ArgumentException("projectId inexistente.");
                }
            }

            var nextOrder = await _roadmapRepository.GetNextOrderAsync(dto.PortfolioId);

            var item = new RoadmapItem
            {
                PortfolioId = dto.PortfolioId,
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,
                Order = nextOrder,
            };

            await _roadmapRepository.AddAsync(item);
            var created = await _roadmapRepository.GetByIdAsync(item.Id);

            return MapToDto(created ?? item);
        }

        public async Task UpdateAsync(UpdateRoadmapItemDto dto)
        {
            var existing = await _roadmapRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException("Item de roadmap não encontrado.");
            }

            ValidateTitle(dto.Title);
            ValidateDates(dto.StartDate, dto.EndDate);

            if (dto.ProjectId.HasValue)
            {
                var projectExists = await _projectRepository.ExistsAsync(dto.ProjectId.Value);
                if (!projectExists)
                {
                    throw new ArgumentException("projectId inexistente.");
                }
            }

            existing.ProjectId = dto.ProjectId;
            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.StartDate = dto.StartDate;
            existing.EndDate = dto.EndDate;
            existing.Status = dto.Status;
            existing.Order = dto.Order;
            existing.UpdatedDate = DateTime.UtcNow;

            await _roadmapRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _roadmapRepository.DeleteAsync(id);
        }

        public async Task ReorderAsync(Guid portfolioId, ReorderRoadmapDto dto)
        {
            var items = (await _roadmapRepository.GetByPortfolioIdAsync(portfolioId)).ToList();
            var itemsById = items.ToDictionary(i => i.Id);

            if (dto.OrderedIds == null || dto.OrderedIds.Count == 0)
            {
                throw new ArgumentException("orderedIds não pode ser vazio.");
            }

            foreach (var id in dto.OrderedIds)
            {
                if (!itemsById.ContainsKey(id))
                {
                    throw new ArgumentException($"O id {id} não pertence ao portfólio informado.");
                }
            }

            for (var i = 0; i < dto.OrderedIds.Count; i++)
            {
                var item = itemsById[dto.OrderedIds[i]];
                item.Order = i + 1;
                item.UpdatedDate = DateTime.UtcNow;
            }

            await _roadmapRepository.UpdateRangeAsync(items);
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            {
                throw new ArgumentException("title é obrigatório e deve ter ao menos 3 caracteres.");
            }
        }

        private static void ValidateDates(DateTime startDate, DateTime endDate)
        {
            // RN12: término igual ou posterior ao início (o front já aceita o mesmo dia).
            if (endDate < startDate)
            {
                throw new ArgumentException("endDate deve ser igual ou posterior a startDate.");
            }
        }

        private static RoadmapItemDto MapToDto(RoadmapItem item)
        {
            return new RoadmapItemDto
            {
                Id = item.Id,
                PortfolioId = item.PortfolioId,
                ProjectId = item.ProjectId,
                ProjectName = item.Project?.Name,
                Title = item.Title,
                Description = item.Description,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                Order = item.Order,
                CreatedAt = item.CreatedDate,
            };
        }
    }
}
