namespace Prumo.Application.DTOs.Portfolio
{
    // POST /portfolios e PUT /portfolios/{id} — { nome, descricao, objetivo, responsavelId }
    public class CreatePortfolioDto
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public string? Objetivo { get; set; }
        public Guid? ResponsavelId { get; set; }
    }
}
