namespace Prumo.Application.DTOs.Project
{
    // Linha da listagem de projetos do portfólio.
    public class ProjetoResumoDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid ResponsavelId { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
    }
}
