using System;

namespace Prumo.Application.DTOs.PriorityCriteria
{
    public class PriorityCriteriaDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ValueWeight { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid UserId { get; set; }
    }
}