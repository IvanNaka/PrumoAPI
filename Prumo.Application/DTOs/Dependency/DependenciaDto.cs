namespace Prumo.Application.DTOs.Dependency
{
    public class DependenciaDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public Guid ProjetoOrigemId { get; set; }
        public string ProjetoOrigemNome { get; set; } = string.Empty;
        public string ProjetoOrigemStatus { get; set; } = string.Empty;
        public Guid ProjetoDestinoId { get; set; }
        public string ProjetoDestinoNome { get; set; } = string.Empty;
        public string ProjetoDestinoStatus { get; set; } = string.Empty;
        public string? Descricao { get; set; }

        /// <summary>F13.</summary>
        public bool EmRisco { get; set; }
        public string? Motivo { get; set; }
    }

    // POST /dependencias — { projetoOrigemId, projetoDestinoId, descricao }
    public class CriarDependenciaDto
    {
        public Guid? ProjetoOrigemId { get; set; }
        public Guid? ProjetoDestinoId { get; set; }
        public string? Descricao { get; set; }
    }
}
