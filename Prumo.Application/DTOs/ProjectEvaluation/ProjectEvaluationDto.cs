using Prumo.Domain.Entities;

namespace Prumo.Application.DTOs.ProjectEvaluation
{
    public class ProjectEvaluationDto
    {
        public Guid Id { get; set; }
        public Guid PriorityCriteriaId { get; set; }
        public string? PriorityCriteriaName { get; set; }
        public Guid UserId { get; set; }
        public decimal Value { get; set; }
        public decimal Weight { get; set; }
    }
}