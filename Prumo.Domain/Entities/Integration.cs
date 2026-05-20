using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Domain.Entities
{
    public class Integration : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public IntegrationType Type { get; set; } // Jira, AzureDevOps, GitHub

        public string ApiUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
