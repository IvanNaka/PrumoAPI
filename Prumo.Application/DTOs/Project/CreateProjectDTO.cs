using Prumo.Application.DTOs.ProjectEvaluation;
using Prumo.Domain.Enums;

namespace Prumo.Application.DTOs.Project
{
    public class CreateProjectDTO
    {

        public Guid PortfolioId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }

        public List<CreateProjectEvaluationDto>? CriteriaScores { get; set; }

    }
}
