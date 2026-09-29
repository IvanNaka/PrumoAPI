namespace Prumo.Application.DTOs.Portfolio
{
    public class PortfolioDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? Objetivo { get; set; }
        public Guid ResponsavelId { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public int QuantidadeProjetos { get; set; }
        public int QuantidadeCriterios { get; set; }
        public int QuantidadeMembros { get; set; }
    }

    public class PortfolioMemberDto
    {
        public Guid UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Perfis { get; set; } = new();
        public bool Responsavel { get; set; }
    }

    public class AddPortfolioMemberDto
    {
        public Guid UsuarioId { get; set; }
    }
}
