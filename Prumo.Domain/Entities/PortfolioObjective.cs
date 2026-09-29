namespace Prumo.Domain.Entities
{
    // PortfolioOkr (PortfolioId, OkrId) — associação exigida pelo roteiro do Cap. 4.
    public class PortfolioObjective
    {
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        public Guid ObjectiveId { get; set; }
        public Objective Objective { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
