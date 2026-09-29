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

    public class BusinessCaseDto
    {
        public Guid? Id { get; set; }
        public Guid ProjetoId { get; set; }
        public bool Cadastrado { get; set; }
        public decimal InvestimentoInicial { get; set; }
        public decimal TaxaDescontoAnual { get; set; }
        public List<FluxoCaixaDto> FluxosPrevistos { get; set; } = new();
    }

    public class FluxoCaixaDto
    {
        public int Mes { get; set; }
        public decimal Valor { get; set; }
    }

    // PUT /projetos/{id}/business-case — substitui a lista de fluxos inteira.
    public class SalvarBusinessCaseDto
    {
        public decimal? InvestimentoInicial { get; set; }
        public decimal? TaxaDescontoAnual { get; set; }
        public List<FluxoCaixaDto>? FluxosPrevistos { get; set; }
    }

    public class RetornoDto
    {
        public Guid Id { get; set; }
        public Guid ProjetoId { get; set; }
        public DateOnly Data { get; set; }
        public decimal Valor { get; set; }
        public string? Descricao { get; set; }
    }

    // POST /projetos/{id}/retornos
    public class SalvarRetornoDto
    {
        public DateOnly? Data { get; set; }
        public decimal? Valor { get; set; }
        public string? Descricao { get; set; }
    }
}
