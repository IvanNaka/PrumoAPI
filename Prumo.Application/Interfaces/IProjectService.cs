using Prumo.Application.DTOs.Project;
using Prumo.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    public interface IProjectService
    {
        Task<ProjectDto> GetByIdAsync(Guid id);
        Task<IEnumerable<ProjectDto>> GetAllAsync();
        Task<IEnumerable<ProjectDto>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<IEnumerable<ProjectDto>> GetByOwnerIdAsync(Guid ownerId);
        Task<ProjectDto> AddAsync(CreateProjectDTO project);
        Task UpdateAsync(Project project);
        Task DeleteAsync(Guid id);
    }
}