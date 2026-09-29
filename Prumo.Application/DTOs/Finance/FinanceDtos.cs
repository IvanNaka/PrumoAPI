namespace Prumo.Application.DTOs.Finance
{
    public class LancamentoDto
    {
        public Guid Id { get; set; }
        public Guid ProjetoId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public DateOnly DataLancamento { get; set; }
    }

    // POST /projetos/{id}/lancamentos — { descricao, valor, tipo, dataLancamento }
    public class SalvarLancamentoDto
    {
        public string? Descricao { get; set; }
        public decimal? Valor { get; set; }
        public string? Tipo { get; set; }
        public DateOnly? DataLancamento { get; set; }
    }
}
