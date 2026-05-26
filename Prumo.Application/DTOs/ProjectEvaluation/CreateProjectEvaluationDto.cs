namespace Prumo.Application.DTOs.ProjectEvaluation
{
    public class CreateProjectEvaluationDto
    {
        public Guid PriorityCriteriaId { get; set; }
        public Guid? UserId { get; set; }
        public decimal Value { get; set; }
    }
}