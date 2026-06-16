using System;

namespace Prumo.Application.DTOs.Portfolio
{
    public class UpdatePortfolioDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}