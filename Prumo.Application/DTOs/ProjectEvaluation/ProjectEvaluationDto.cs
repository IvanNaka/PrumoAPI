namespace Prumo.Application.DTOs.ProjectEvaluation
{
    public class ProjectEvaluationDto
    {
        public Guid Id { get; set; }
        public Guid PriorityCriteriaId { get; set; }
        public Guid UserId { get; set; }
        public decimal Value { get; set; }
    }
}