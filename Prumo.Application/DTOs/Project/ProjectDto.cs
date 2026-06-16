using Prumo.Application.DTOs.ProjectEvaluation;
using System;
using System.Collections.Generic;

namespace Prumo.Application.DTOs.Project
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string PortfolioName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; }

        public DateTime CreatedDate { get; set; }

        public List<ProjectEvaluationDto>? ProjectEvaluations { get; set; }
    }
}