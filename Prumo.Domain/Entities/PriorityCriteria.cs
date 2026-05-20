namespace Prumo.Domain.Entities
{
    public class PriorityCriteria : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public decimal ValueWeight { get; set; } = 0;
        public decimal EffortWeight { get; set; } = 0;
        public decimal RiskWeight { get; set; } = 0;
        public decimal AlignmentWeight { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
