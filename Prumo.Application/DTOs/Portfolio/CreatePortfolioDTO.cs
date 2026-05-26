using System;

namespace Prumo.Application.DTOs.Portfolio
{
    public class CreatePortfolioDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
    }
}