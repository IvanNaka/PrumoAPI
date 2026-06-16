using System;

namespace Prumo.Application.DTOs.PriorityCriteria
{
    public class CreatePriorityCriteriaDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal ValueWeight { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid UserId { get; set; }
    }
}