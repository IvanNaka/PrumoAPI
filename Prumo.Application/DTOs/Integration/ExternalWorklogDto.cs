using System;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>
    /// Normalized worklog entry: "registro das horas trabalhadas ou esforço aplicado em uma
    /// atividade", used to compute Burn Rate and effort/capacity indicators.
    /// </summary>
    public class ExternalWorklogDto
    {
        public string IssueExternalId { get; set; }
        public string UserName { get; set; }
        public double HoursSpent { get; set; }
        public DateTime LoggedAt { get; set; }
    }
}
