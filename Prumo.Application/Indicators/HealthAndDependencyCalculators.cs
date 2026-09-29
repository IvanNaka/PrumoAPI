using Prumo.Application.Indicators.Models;
using Prumo.Domain.Enums;

namespace Prumo.Application.Indicators
{
    public record DependencyProject(Guid Id, string Name, ProjectStatus Status, DateOnly EndDate);

    public record DependencyRisk(bool EmRisco, string? Motivo);

    public record HealthProjectInput(
        Guid Id,
        string Name,
        ProjectStatus Status,
        DateOnly EndDate,
        decimal? PercentualConsumido,
        int TotalIssues,
        int ConcludedIssues,
        int LateIssues,
        bool HasDependencyAtRisk);

    public class ProjectHealth : IndicatorResult
    {
        public Guid ProjetoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalIssues { get; set; }
        public int Concluidas { get; set; }
        public int Atrasadas { get; set; }
        public decimal? Progresso { get; set; }
        public List<string> Flags { get; set; } = new();
        public int PontuacaoSaude { get; set; }
        public string Classificacao { get; set; } = string.Empty;
    }

    public class HealthResult : IndicatorResult
    {
        public decimal Saude { get; set; }
        public string? Classificacao { get; set; }
        public Dictionary<string, int> Contagem { get; set; } = new()
        {
            [HealthCalculator.Saudavel] = 0,
            [HealthCalculator.Atencao] = 0,
            [HealthCalculator.Critico] = 0,
        };
        public List<ProjectHealth> Projetos { get; set; } = new();
    }

    /// <summary>F13 — Dependência em risco (RF28).</summary>
    public static class DependencyRiskCalculator
    {
        /// <summary>
        /// Origem depende de Destino. Em risco quando Destino.Status ∈ {EmRisco, Suspenso, Cancelado}
        /// OU (Destino.Status != Concluido E Destino.DataFim > Origem.DataFim).
        /// </summary>
        public static DependencyRisk Avaliar(DependencyProject origem, DependencyProject destino)
        {
            if (destino.Status is ProjectStatus.EmRisco or ProjectStatus.Suspenso or ProjectStatus.Cancelado)
            {
                return new DependencyRisk(true, $"Projeto dependente está {StatusTexto(destino.Status)}");
            }

            if (destino.Status != ProjectStatus.Concluido && destino.EndDate > origem.EndDate)
            {
                return new DependencyRisk(true, "Projeto dependente termina depois deste projeto");
            }

            return new DependencyRisk(false, null);
        }

        private static string StatusTexto(ProjectStatus status) => status switch
        {
            ProjectStatus.EmRisco => "em risco",
            ProjectStatus.Suspenso => "suspenso",
            ProjectStatus.Cancelado => "cancelado",
            _ => status.ToString(),
        };
    }

    /// <summary>F14 — Detecção de ciclo (RN19).</summary>
    public static class DependencyCycleDetector
    {
        /// <summary>
        /// Antes de gravar Origem -> Destino, busca em profundidade a partir de Destino seguindo as
        /// dependências existentes; se chegar em Origem, a gravação criaria um ciclo.
        /// </summary>
        public static bool CriaCiclo(Guid origem, Guid destino, ILookup<Guid, Guid> arestas)
        {
            var pilha = new Stack<Guid>(new[] { destino });
            var visitados = new HashSet<Guid>();
            while (pilha.Count > 0)
            {
                var atual = pilha.Pop();
                if (atual == origem) return true;
                if (!visitados.Add(atual)) continue;
                foreach (var prox in arestas[atual]) pilha.Push(prox);
            }
            return false;
        }
    }

    /// <summary>F12 — Saúde do projeto e do portfólio (RF35).</summary>
    public static class HealthCalculator
    {
        public const string Saudavel = "Saudável";
        public const string Atencao = "Atenção";
        public const string Critico = "Crítico";

        public const string FlagEmRisco = "Projeto com status Em risco";
        public const string FlagOrcamento = "Orçamento consumido acima de 100%";
        public const string FlagPrazo = "Data de término ultrapassada";
        public const string FlagAtrasadas = "Mais de 20% das tarefas atrasadas";
        public const string FlagDependencia = "Depende de projeto em risco";

        /// <summary>Somente projetos Planejado, EmAndamento ou EmRisco são avaliados.</summary>
        public static bool Avaliavel(ProjectStatus status) =>
            status is ProjectStatus.Planejado or ProjectStatus.EmAndamento or ProjectStatus.EmRisco;

        public static string Classificar(decimal pontuacao) =>
            pontuacao >= 75 ? Saudavel : pontuacao >= 50 ? Atencao : Critico;

        public static ProjectHealth CalcularProjeto(HealthProjectInput p, DateOnly hoje)
        {
            var flags = new List<string>();
            if (p.Status == ProjectStatus.EmRisco) flags.Add(FlagEmRisco);                                        // (a)
            if (p.PercentualConsumido.HasValue && p.PercentualConsumido.Value > 100) flags.Add(FlagOrcamento);     // (b)
            if (hoje > p.EndDate) flags.Add(FlagPrazo);                                                             // (c)
            if (p.TotalIssues > 0 && (decimal)p.LateIssues / p.TotalIssues > 0.20m) flags.Add(FlagAtrasadas);       // (d)
            if (p.HasDependencyAtRisk) flags.Add(FlagDependencia);                                                  // (e)

            var pontuacao = Math.Max(0, 100 - 25 * flags.Count);
            return new ProjectHealth
            {
                ProjetoId = p.Id,
                Nome = p.Name,
                Status = p.Status.ToString(),
                TotalIssues = p.TotalIssues,
                Concluidas = p.ConcludedIssues,
                Atrasadas = p.LateIssues,
                Progresso = p.TotalIssues > 0 ? IndicatorMath.R2((decimal)p.ConcludedIssues / p.TotalIssues * 100m) : null,
                Flags = flags,
                PontuacaoSaude = pontuacao,
                Classificacao = Classificar(pontuacao),
            };
        }

        /// <summary>Portfólio: saude = média(pontuacaoSaude dos projetos avaliados) + contagem por classificação.</summary>
        public static HealthResult CalcularPortfolio(IEnumerable<HealthProjectInput> projetos, DateOnly hoje)
        {
            var avaliados = projetos.Where(p => Avaliavel(p.Status)).Select(p => CalcularProjeto(p, hoje)).ToList();
            var result = new HealthResult { Projetos = avaliados.OrderBy(p => p.PontuacaoSaude).ToList() };
            if (avaliados.Count == 0)
            {
                return result.Indisponivel<HealthResult>();
            }

            result.Saude = IndicatorMath.R2((decimal)avaliados.Average(p => p.PontuacaoSaude));
            result.Classificacao = Classificar(result.Saude);
            foreach (var projeto in avaliados)
            {
                result.Contagem[projeto.Classificacao]++;
            }

            return result;
        }
    }
}
