using Prumo.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prumo.Application.DTOs.Project
{
    public class GetProjectDTO
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Portfolio relation
        public Guid PortfolioId { get; set; }
        public string PortfolioName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }
}
