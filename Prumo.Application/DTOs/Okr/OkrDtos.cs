namespace Prumo.Application.DTOs.Okr
{
    public class OkrDto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        /// <summary>F4: média do progresso dos KRs.</summary>
        public decimal Progresso { get; set; }
        public int QuantidadeProjetos { get; set; }
        public List<KeyResultDto> KeyResults { get; set; } = new();
    }

    public class KeyResultDto
    {
        public Guid Id { get; set; }
        public Guid OkrId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Meta { get; set; }
        public decimal ValorAtual { get; set; }

        /// <summary>F4: min(ValorAtual / Meta, 1) × 100.</summary>
        public decimal Progresso { get; set; }
    }

    // POST /okrs e PUT /okrs/{id} — { titulo, descricao, dataInicio, dataFim, keyResults:[{ id?, descricao, meta, valorAtual }] }
    public class SalvarOkrDto
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }
        public List<SalvarKeyResultDto>? KeyResults { get; set; }
    }

    public class SalvarKeyResultDto
    {
        public Guid? Id { get; set; }
        public string? Descricao { get; set; }
        public decimal? Meta { get; set; }
        public decimal? ValorAtual { get; set; }
    }

    // PUT /key-results/{id} — { valorAtual }
    public class AtualizarValorKrDto
    {
        public decimal? ValorAtual { get; set; }
    }

    /// <summary>OKR resumido para as associações (projeto e portfólio).</summary>
    public class OkrResumoDto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public decimal Progresso { get; set; }
    }
}
