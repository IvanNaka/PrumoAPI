using System;
using Prumo.Domain.Enums;

namespace Prumo.Application.DTOs.Roadmap
{
    public class RoadmapItemDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public RoadmapStatus Status { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
