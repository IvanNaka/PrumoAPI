namespace Prumo.Application.DTOs.Criteria
{
    // CriterioPrioridade (Seção 3.2).
    public class CriterioDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal Peso { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int QuantidadeAvaliacoes { get; set; }
    }

    // POST /portfolios/{id}/criterios e PUT /criterios/{id} — { nome, descricao, peso, tipo }
    public class SalvarCriterioDto
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal? Peso { get; set; }
        public string? Tipo { get; set; }
    }
}
