namespace Prumo.Domain.Entities
{
    // AvaliacaoCriterio (Seção 3.2, D06): uma nota de 1 a 5 por critério. Único por (Projeto, Critério).
    public class ProjectEvaluation : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PriorityCriteriaId { get; set; }
        public PriorityCriteria PriorityCriteria { get; set; } = null!;

        /// <summary>Avaliador (AvaliadorId).</summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>Nota de 1 a 5.</summary>
        public int Score { get; set; }

        /// <summary>DataAvaliacao.</summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
