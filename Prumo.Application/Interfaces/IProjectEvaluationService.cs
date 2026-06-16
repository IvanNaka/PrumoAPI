namespace Prumo.Application.Interfaces
{
    using Prumo.Application.DTOs.ProjectEvaluation;

    public interface IProjectEvaluationService
    {
        Task<ProjectEvaluationDto> GetByIdAsync(Guid id);
        Task<IEnumerable<ProjectEvaluationDto>> GetAllAsync();
        Task<ProjectEvaluationDto> CreateAsync(CreateProjectEvaluationDto dto);
        Task DeleteAsync(Guid id);
    }
}