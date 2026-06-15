using Prumo.Application.DTOs.ProjectDependency;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class ProjectDependencyService : IProjectDependencyService
    {
        private readonly IProjectDependencyRepository _repository;

        public ProjectDependencyService(IProjectDependencyRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProjectDependencyDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            return MapToDto(entity);
        }

        public async Task<IEnumerable<ProjectDependencyDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToDto);
        }
        public async Task<IEnumerable<ProjectDependencyDto>> GetAllByPortfolioAsync(Guid portfolioId)
        {
            var entities = await _repository.GetAllByPortfolioAsync(portfolioId);
            return entities.Select(MapToDto);
        }

        public async Task<ProjectDependencyDto> CreateAsync(CreateProjectDependencyDto dto)
        {
            var entity = new ProjectDependency
            {
                Reason = dto.Reason,
                ProjectId = dto.ProjectId,
                DependsOnProjectId = dto.DependsOnProjectId,
                PortfolioId = dto.PortfolioId,
                UserId = dto.UserId,
            };

            await _repository.AddAsync(entity);

            return MapToDto(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }

        private static ProjectDependencyDto MapToDto(ProjectDependency entity)
        {
            return new ProjectDependencyDto
            {
                Id = entity.Id,
                Reason = entity.Reason,
                ProjectId = entity.ProjectId,
                ProjectName = entity.Project?.Name,
                PortfolioId = entity.PortfolioId,
                PortfolioName = entity.Portfolio?.Name,
                DependsOnProjectId = entity.DependsOnProjectId,
                DependsOnProjectName = entity.DependsOnProject?.Name,
                UserId = entity.UserId,
                UserName = entity.User?.Name,
            };
        }
    }
}