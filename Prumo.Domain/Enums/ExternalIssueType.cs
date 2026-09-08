using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Enums
{
    /// <summary>
    /// Normalized item type across external tools (Jira issues, Azure DevOps work items,
    /// GitHub issues/PRs, Trello cards), used to compute quality indicators (Bug vs Feature ratio).
    /// </summary>
    public enum ExternalIssueType
    {
        Story,
        Task,
        Feature,
        Bug,
        Other
    }
}
