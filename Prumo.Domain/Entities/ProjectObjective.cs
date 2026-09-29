namespace Prumo.Domain.Entities
{
    // ProjetoOkr (ProjetoId, OkrId): chave composta; o mesmo par não se repete (RF16).
    public class ProjectObjective : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public Guid ObjectiveId { get; set; }
        public Objective Objective { get; set; } = null!;
    }
}
