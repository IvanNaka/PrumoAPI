namespace Prumo.Application.DTOs.Project
{
    /// <summary>
    /// GET /projetos/{id} — detalhe completo do projeto (roteiro do Cap. 4, Parte 2): dados, notas
    /// por critério, score, OKRs, dependências, resumo financeiro e indicadores.
    /// </summary>
    public class ProjetoDetalheDto : ProjetoResumoDto
    {
        public string? Descricao { get; set; }
        public string PortfolioNome { get; set; } = string.Empty;
        public string PortfolioStatus { get; set; } = string.Empty;
        public string? JiraProjectKey { get; set; }
        public DateTime? DataConclusao { get; set; }
        public DateTime? DataUltimaPriorizacao { get; set; }

        /// <summary>Ações de status válidas agora (Figura 26).</summary>
        public List<string> AcoesPermitidas { get; set; } = new();

        /// <summary>Notas por critério (1 a 5); nota nula = ainda não avaliado naquele critério.</summary>
        public List<NotaCriterioDto> Avaliacoes { get; set; } = new();

        /// <summary>OKRs associados (RF16) com o progresso F4.</summary>
        public List<Prumo.Application.DTOs.Okr.OkrResumoDto> Okrs { get; set; } = new();

        /// <summary>Dependências em que o projeto é origem ou destino, com o risco (F13).</summary>
        public List<Prumo.Application.DTOs.Dependency.DependenciaDto> Dependencias { get; set; } = new();

        /// <summary>Equipes alocadas ao projeto.</summary>
        public List<EquipeAlocadaDto> Equipes { get; set; } = new();
    }

    public class EquipeAlocadaDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int QuantidadeMembros { get; set; }
        public int CapacidadeMensalTotal { get; set; }
    }

    public class NotaCriterioDto
    {
        public Guid CriterioId { get; set; }
        public string CriterioNome { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int? Nota { get; set; }
        public string? AvaliadorNome { get; set; }
        public DateTime? DataAvaliacao { get; set; }
    }

    // POST /portfolios/{id}/projetos e PUT /projetos/{id}
    public class SalvarProjetoDto
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public Guid? ResponsavelId { get; set; }
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }
        public decimal? OrcamentoAprovado { get; set; }
        public string? CategoriaEstrategica { get; set; }
        public string? Prioridade { get; set; }
        public string? JiraProjectKey { get; set; }
    }

    // POST /projetos/{id}/status — { acao }
    public class AlterarStatusProjetoDto
    {
        public string? Acao { get; set; }
    }
}
