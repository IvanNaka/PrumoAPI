namespace Prumo.Domain.Entities
{
    // DependenciaProjeto (RF26–RF28): ProjectId (origem) depende de DependsOnProjectId (destino).
    // Índice único (origem, destino); origem diferente de destino; sem ciclos (F14).
    public class ProjectDependency : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Descricao (opcional, 500).</summary>
        public string? Reason { get; set; }

        /// <summary>Usuário que cadastrou.</summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } = null!;

        /// <summary>ProjetoOrigemId: o projeto que depende.</summary>
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>ProjetoDestinoId: o projeto do qual a origem depende.</summary>
        public Guid DependsOnProjectId { get; set; }
        public Project DependsOnProject { get; set; } = null!;
    }
}
