using System;

namespace Prumo.Application.DTOs.ProjectDependency
{
    public class ProjectDependencyDto
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public Guid DependsOnProjectId { get; set; }
        public string DependsOnProjectName { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public Guid PortfolioId { get; set; }
        public string PortfolioName { get; set; }
    }
}