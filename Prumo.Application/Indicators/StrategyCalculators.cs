using Prumo.Application.Indicators.Models;
using Prumo.Domain.Enums;

namespace Prumo.Application.Indicators
{
    public record KeyResultValue(decimal Target, decimal Current);

    public record ProjectBudget(Guid Id, string Name, StrategicCategory Category, decimal ApprovedBudget, IReadOnlyCollection<Guid> ObjectiveIds);

    public record ObjectiveProgressInput(Guid Id, string Title, IReadOnlyCollection<KeyResultValue> KeyResults);

    public class AllocationItem
    {
        public string Categoria { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Orcamento { get; set; }
        public decimal PercentualOrcamento { get; set; }
    }

    public class AllocationResult : IndicatorResult
    {
        public decimal OrcamentoTotal { get; set; }
        public List<AllocationItem> Categorias { get; set; } = new();
    }

    public class OkrProgressItem
    {
        public Guid OkrId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public decimal Progresso { get; set; }
    }

    public class UnalignedProject
    {
        public Guid ProjetoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal OrcamentoAprovado { get; set; }
    }

    public class OkrAlignmentResult : IndicatorResult
    {
        public int TotalProjetos { get; set; }
        public int ProjetosAlinhados { get; set; }
        public decimal PercentualProjetosAlinhados { get; set; }
        public decimal? PercentualOrcamentoAlinhado { get; set; }
        public List<UnalignedProject> ListaDesalinhados { get; set; } = new();
        public List<OkrProgressItem> ProgressoPorOkr { get; set; } = new();
    }

    /// <summary>F4 — Progresso de OKR (RF17).</summary>
    public static class OkrProgressCalculator
    {
        /// <summary>progressoKR = min(ValorAtual / Meta, 1) × 100</summary>
        public static decimal KeyResult(decimal target, decimal current)
        {
            if (target <= 0)
            {
                return 0;
            }

            return IndicatorMath.R2(Math.Min(Math.Max(current, 0) / target, 1m) * 100m);
        }

        /// <summary>progressoOKR = média(progressoKR de todos os KRs do OKR)</summary>
        public static decimal Objective(IReadOnlyCollection<KeyResultValue> keyResults)
        {
            if (keyResults.Count == 0)
            {
                return 0;
            }

            return IndicatorMath.R2(keyResults.Average(k => KeyResult(k.Target, k.Current)));
        }
    }

    /// <summary>F10 — Alocação estratégica Run/Grow/Transform (RF37).</summary>
    public static class AllocationCalculator
    {
        public static AllocationResult Calcular(IReadOnlyCollection<ProjectBudget> activeProjects)
        {
            var result = new AllocationResult();
            var total = activeProjects.Sum(p => p.ApprovedBudget);
            result.OrcamentoTotal = IndicatorMath.R2(total);

            result.Categorias = Enum.GetValues<StrategicCategory>()
                .Select(c =>
                {
                    var projetos = activeProjects.Where(p => p.Category == c).ToList();
                    var orcamento = projetos.Sum(p => p.ApprovedBudget);
                    return new AllocationItem
                    {
                        Categoria = c.ToString(),
                        Quantidade = projetos.Count,
                        Orcamento = IndicatorMath.R2(orcamento),
                        PercentualOrcamento = total > 0 ? IndicatorMath.R2(orcamento / total * 100m) : 0,
                    };
                })
                .ToList();

            if (activeProjects.Count == 0 || total <= 0)
            {
                return result.Indisponivel<AllocationResult>();
            }

            return result;
        }
    }

    /// <summary>F11 — Alinhamento a OKRs (RF42).</summary>
    public static class OkrAlignmentCalculator
    {
        public static OkrAlignmentResult Calcular(
            IReadOnlyCollection<ProjectBudget> activeProjects,
            IReadOnlyCollection<ObjectiveProgressInput> portfolioObjectives)
        {
            var result = new OkrAlignmentResult
            {
                TotalProjetos = activeProjects.Count,
                ProgressoPorOkr = portfolioObjectives
                    .Select(o => new OkrProgressItem
                    {
                        OkrId = o.Id,
                        Titulo = o.Title,
                        Progresso = OkrProgressCalculator.Objective(o.KeyResults),
                    })
                    .ToList(),
            };

            if (activeProjects.Count == 0)
            {
                return result.Indisponivel<OkrAlignmentResult>();
            }

            var alinhados = activeProjects.Where(p => p.ObjectiveIds.Count > 0).ToList();
            var orcamentoTotal = activeProjects.Sum(p => p.ApprovedBudget);

            result.ProjetosAlinhados = alinhados.Count;
            result.PercentualProjetosAlinhados = IndicatorMath.R2((decimal)alinhados.Count / activeProjects.Count * 100m);
            result.PercentualOrcamentoAlinhado = orcamentoTotal > 0
                ? IndicatorMath.R2(alinhados.Sum(p => p.ApprovedBudget) / orcamentoTotal * 100m)
                : null;
            result.ListaDesalinhados = activeProjects
                .Where(p => p.ObjectiveIds.Count == 0)
                .Select(p => new UnalignedProject { ProjetoId = p.Id, Nome = p.Name, OrcamentoAprovado = p.ApprovedBudget })
                .ToList();
            return result;
        }
    }
}
