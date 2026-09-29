using Prumo.Domain.Enums;
using System;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Normalized representation of a work item from any external tool (Jira issue,
    /// Azure DevOps work item, GitHub issue/PR, Trello card). Used to feed indicators
    /// such as Lead Time (CreatedAt -> ResolvedAt) and Bug vs Feature quality metrics.
    /// </summary>
    public class ExternalIssueDto
    {
        public string ExternalId { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public ExternalIssueType? Type { get; set; }
        public string Status { get; set; }
        public string ProjectExternalId { get; set; }
        public string AssigneeName { get; set; }

        /// <summary>Original estimate, in hours.</summary>
        public double? EstimateHours { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Completion date, used to compute Lead Time. Null while still open.</summary>
        public DateTime? ResolvedAt { get; set; }
    }
}
