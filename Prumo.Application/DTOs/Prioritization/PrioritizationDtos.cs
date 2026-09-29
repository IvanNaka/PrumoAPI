namespace Prumo.Application.DTOs.Prioritization
{
    // PUT /projetos/{id}/avaliacoes — [{ criterioId, nota }]
    public class NotaInputDto
    {
        public Guid CriterioId { get; set; }
        public int? Nota { get; set; }
    }

    public class AvaliacaoProjetoDto
    {
        public Guid ProjetoId { get; set; }
        public string StatusAvaliacao { get; set; } = string.Empty;
        public decimal? ScoreAtual { get; set; }
        public int? PosicaoRanking { get; set; }
        public bool Completa { get; set; }
        public List<NotaDto> Notas { get; set; } = new();
    }

    public class NotaDto
    {
        public Guid CriterioId { get; set; }
        public int Nota { get; set; }
    }

    /// <summary>POST /portfolios/{id}/priorizacao e GET /portfolios/{id}/ranking.</summary>
    public class PriorizacaoResultadoDto
    {
        public DateTime? DataUltimaPriorizacao { get; set; }
        public string PortfolioStatus { get; set; } = string.Empty;
        public List<RankingItemDto> Ranking { get; set; } = new();
        public List<NaoAvaliadoDto> NaoAvaliados { get; set; } = new();
    }

    public class RankingItemDto
    {
        public int Posicao { get; set; }
        public Guid ProjetoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Prioridade { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusAvaliacao { get; set; } = string.Empty;
    }

    public class NaoAvaliadoDto
    {
        public Guid ProjetoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string StatusAvaliacao { get; set; } = string.Empty;
        public List<string> CriteriosFaltantes { get; set; } = new();
    }

    /// <summary>Matriz da aba "Avaliar": projetos x critérios.</summary>
    public class MatrizAvaliacaoDto
    {
        public List<CriterioColunaDto> Criterios { get; set; } = new();
        public List<AvaliacaoProjetoLinhaDto> Projetos { get; set; } = new();
    }

    public class CriterioColunaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    public class AvaliacaoProjetoLinhaDto
    {
        public Guid ProjetoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusAvaliacao { get; set; } = string.Empty;
        public decimal? ScoreAtual { get; set; }
        public Dictionary<Guid, int> Notas { get; set; } = new();
    }
}
