using Prumo.Application.Indicators.Models;
using Prumo.Domain.Enums;

namespace Prumo.Application.Indicators
{
    public record IssueData(
        ExternalIssueType Type,
        bool Done,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        DateOnly? DueDate,
        decimal? EstimateHours,
        decimal SpentHours,
        string? AssigneeEmail);

    public class LeadTimeResult : IndicatorResult
    {
        public int Quantidade { get; set; }
        public decimal LeadTimeMediano { get; set; }
        public decimal LeadTimeMedio { get; set; }
        public decimal P85 { get; set; }
    }

    public class QualityResult : IndicatorResult
    {
        public int Bugs { get; set; }
        public int Entregas { get; set; }
        public decimal RazaoBugsPorEntrega { get; set; }
        public decimal IndiceQualidade { get; set; }
        public decimal MetaRazao { get; set; } = QualityCalculator.MetaRazao;
        public bool AtingiuMeta { get; set; }
        public Dictionary<string, int> PorTipo { get; set; } = new();
    }

    /// <summary>F7 — Lead Time (RF40).</summary>
    public static class LeadTimeCalculator
    {
        /// <summary>
        /// Para cada issue com Concluida == true e DataConclusao dentro do período:
        /// lt = (DataConclusao - DataCriacao).TotalDays. Indicador principal: mediana.
        /// </summary>
        public static LeadTimeResult Calcular(IEnumerable<IssueData> issues, DateTime de, DateTime ate)
        {
            var durations = issues
                .Where(i => i.Done && i.CompletedAt.HasValue && i.CompletedAt.Value >= de && i.CompletedAt.Value <= ate)
                .Select(i => (i.CompletedAt!.Value - i.CreatedAt).TotalDays)
                .ToList();

            return FromDurations(durations);
        }

        public static LeadTimeResult FromDurations(IReadOnlyList<double> durations)
        {
            var result = new LeadTimeResult { Quantidade = durations.Count };
            if (durations.Count == 0)
            {
                return result.Indisponivel<LeadTimeResult>();
            }

            result.LeadTimeMediano = IndicatorMath.R2(IndicatorMath.Median(durations));
            result.LeadTimeMedio = IndicatorMath.R2(durations.Average());
            result.P85 = IndicatorMath.R2(IndicatorMath.Percentile(durations, 85));
            return result;
        }
    }

    /// <summary>F8 — Qualidade da entrega (RF33, RF34, RF38).</summary>
    public static class QualityCalculator
    {
        /// <summary>Meta fixa da razão bugs/entregas.</summary>
        public const decimal MetaRazao = 0.20m;

        public static QualityResult Calcular(IEnumerable<IssueData> issues, DateTime de, DateTime ate)
        {
            var concluidas = issues
                .Where(i => i.Done && i.CompletedAt.HasValue && i.CompletedAt.Value >= de && i.CompletedAt.Value <= ate)
                .ToList();

            var result = FromCounts(
                concluidas.Count(i => i.Type == ExternalIssueType.Bug),
                concluidas.Count(i => i.Type != ExternalIssueType.Bug));
            result.PorTipo = Enum.GetValues<ExternalIssueType>()
                .ToDictionary(t => t.ToString(), t => concluidas.Count(i => i.Type == t));
            return result;
        }

        /// <summary>razao = bugs / entregas (entregas = 0 -> indisponível); indice = entregas / (entregas + bugs) × 100.</summary>
        public static QualityResult FromCounts(int bugs, int entregas)
        {
            var result = new QualityResult { Bugs = bugs, Entregas = entregas };
            if (entregas == 0)
            {
                return result.Indisponivel<QualityResult>();
            }

            var razao = (decimal)bugs / entregas;
            result.RazaoBugsPorEntrega = IndicatorMath.R2(razao);
            result.IndiceQualidade = IndicatorMath.R2((decimal)entregas / (entregas + bugs) * 100m);
            result.AtingiuMeta = razao <= MetaRazao;
            return result;
        }
    }
}
