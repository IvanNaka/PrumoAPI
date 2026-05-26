namespace Prumo.Domain.Entities
{
    public class PriorityCriteria : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();


        public decimal ValueWeight { get; set; } = 0;
        public string Name { get; set; } = "";
        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

    }
}
