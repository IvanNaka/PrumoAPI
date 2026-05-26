using System;

namespace Prumo.Application.DTOs.PriorityCriteria
{
    public class UpdatePriorityCriteriaDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ValueWeight { get; set; }
    }
}