namespace Prumo.Application.DTOs.Criteria
{
    // CriterioPrioridade (Seção 3.2).
    public class CriterioDto
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Peso { get; set; }
    }
}
