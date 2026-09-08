using System;
using System.Collections.Generic;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Result of a full sync run (UC16 "Sincronizar Dados"): the normalized data obtained
    /// from the external tool plus how many records were persisted locally.
    /// </summary>
    public class IntegrationSyncResultDto
    {
        public Guid IntegrationId { get; set; }
        public DateTime SyncedAtUtc { get; set; }
        public List<ExternalProjectDto> Projects { get; set; } = new();
        public List<ExternalIssueDto> Issues { get; set; } = new();
        public List<ExternalWorklogDto> Worklogs { get; set; } = new();
        public int IssuesImported { get; set; }
        public int WorklogsImported { get; set; }
    }
}
