namespace Prumo.Domain.Entities
{
    // ProjetoEquipe (ProjetoId, EquipeId): equipes alocadas ao projeto; o mesmo par não se repete.
    public class ProjectTeam : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;
    }
}
