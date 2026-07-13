using Prumo.Application.DTOs.ProjectEvaluation;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class ProjectEvaluationService : IProjectEvaluationService
    {
        private readonly IProjectEvaluationRepository _repository;

        public ProjectEvaluationService(IProjectEvaluationRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProjectEvaluationDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            return MapToDto(entity);
        }

        public async Task<IEnumerable<ProjectEvaluationDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToDto);
        }

        public async Task<ProjectEvaluationDto> CreateAsync(CreateProjectEvaluationDto dto)
        {
            var entity = new ProjectEvaluation
            {
                PriorityCriteriaId = dto.PriorityCriteriaId,
                UserId = (Guid)(dto.UserId),
                Value = dto.Value
            };

            await _repository.AddAsync(entity);

            return MapToDto(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        private static ProjectEvaluationDto MapToDto(ProjectEvaluation entity)
        {
            return new ProjectEvaluationDto
            {
                Id = entity.Id,
                PriorityCriteriaId = entity.PriorityCriteriaId,
                UserId = entity.UserId,
                Value = entity.Value,
                Weight = entity.PriorityCriteria.ValueWeight
            };
        }
    }
}