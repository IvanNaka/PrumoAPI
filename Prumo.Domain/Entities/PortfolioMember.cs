namespace Prumo.Domain.Entities
{
    // PortfolioMembro (PortfolioId, UsuarioId). Define quem pode ver o portfólio (D11).
    public class PortfolioMember
    {
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
