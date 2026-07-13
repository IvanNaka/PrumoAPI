using Newtonsoft.Json.Linq;
using Prumo.Application.DTOs.Project;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prumo.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectEvaluationRepository _projectEvaluationRepository;

        public ProjectService(IProjectRepository projectRepository, IProjectEvaluationRepository projectEvaluationRepository)
        {
            _projectRepository = projectRepository;
            _projectEvaluationRepository = projectEvaluationRepository;
        }

        public async Task<ProjectDto> GetByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null) return null;
            return MapToDto(project);
        }

        public async Task<IEnumerable<ProjectDto>> GetAllAsync()
        {
            var projects = await _projectRepository.GetAllAsync();
            return projects.Select(MapToDto);
        }

        public async Task<IEnumerable<ProjectDto>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            var projects = await _projectRepository.GetByPortfolioIdAsync(portfolioId);
            return projects.Select(MapToDto);
        }

        public async Task<IEnumerable<ProjectDto>> GetByOwnerIdAsync(Guid ownerId)
        {
            var projects = await _projectRepository.GetByOwnerIdAsync(ownerId);
            return projects.Select(MapToDto);
        }

        public async Task<ProjectDto> AddAsync(CreateProjectDTO projectDTO)
        {
            var projectObject = new Project
            {
                PortfolioId = projectDTO.PortfolioId,
                Name = projectDTO.Name,
                Description = projectDTO.Description,
                OwnerId = projectDTO.OwnerId,
                CreatedDate = DateTime.UtcNow
            };

            var projectCreated = await _projectRepository.AddAsync(projectObject);

            if (projectDTO.CriteriaScores != null)
            {
                foreach (var evaluation in projectDTO.CriteriaScores)
                {
                    var ev = new ProjectEvaluation
                    {
                        PriorityCriteriaId = evaluation.PriorityCriteriaId,
                        ProjectId = projectCreated.Id,
                        UserId = projectDTO.OwnerId,
                        Value = evaluation.Value,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _projectEvaluationRepository.AddAsync(ev);
                }
            }

            return MapToDto(projectCreated);
        }

        public async Task UpdateAsync(Project project)
        {
            await _projectRepository.UpdateAsync(project);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _projectRepository.DeleteAsync(id);
        }

        private ProjectDto MapToDto(Project project)
        {
            return new ProjectDto
            {
                Id = project.Id,
                PortfolioId = project.PortfolioId,
                PortfolioName = project.Portfolio?.Name ,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status.ToString(),
                OwnerId = project.OwnerId,
                OwnerName = project.Owner?.Name,
                CreatedDate = project.CreatedDate,
                ProjectEvaluations = project.ProjectEvaluations?.Select(pe => new Prumo.Application.DTOs.ProjectEvaluation.ProjectEvaluationDto
                {
                    Id = pe.Id,
                    PriorityCriteriaId = pe.PriorityCriteriaId,
                    PriorityCriteriaName = pe.PriorityCriteria?.Name,
                    UserId = pe.UserId,
                    Value = pe.Value,
                    Weight = pe.PriorityCriteria.ValueWeight
                }).ToList()
            };
        }
    }
}