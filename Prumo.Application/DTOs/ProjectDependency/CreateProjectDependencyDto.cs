using System;

namespace Prumo.Application.DTOs.ProjectDependency
{
    public class CreateProjectDependencyDto
    {
        public string Reason { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public Guid DependsOnProjectId { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid UserId { get; set; }
    }
}