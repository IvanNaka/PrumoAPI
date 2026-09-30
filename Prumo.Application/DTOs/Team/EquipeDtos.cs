namespace Prumo.Application.DTOs.Team
{
    public class EquipeDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public Guid? PortfolioId { get; set; }
        public string? PortfolioNome { get; set; }
        public int CapacidadeMensalTotal { get; set; }

        /// <summary>Código de convite; só é devolvido para quem pode editar equipes.</summary>
        public string? CodigoConvite { get; set; }
        public List<MembroEquipeDto> Membros { get; set; } = new();
    }

    public class MembroEquipeDto
    {
        public Guid Id { get; set; }
        public Guid EquipeId { get; set; }
        public Guid? UsuarioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal CustoHora { get; set; }
        public int CapacidadeMensalHoras { get; set; }
    }

    // POST/PUT /equipes — { nome, portfolioId }
    public class SalvarEquipeDto
    {
        public string? Nome { get; set; }
        public Guid? PortfolioId { get; set; }
    }

    // POST/PUT /equipes/{id}/membros — { usuarioId, nome, email, custoHora, capacidadeMensalHoras }
    public class SalvarMembroDto
    {
        public Guid? UsuarioId { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public decimal? CustoHora { get; set; }
        public int? CapacidadeMensalHoras { get; set; }
    }
}
