using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F8 — qualidade da entrega no período (RF33, RF34, RF38).</summary>
    public class QualityIndicator : PortfolioIndicator<QualityResult>
    {
        public QualityIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.Qualidade;

        public override async Task<QualityResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectsAsync(portfolioId);
            var issues = await Loader.IssuesAsync(projects.Select(p => p.Id).ToList());
            return QualityCalculator.Calcular(issues.Values.SelectMany(i => i), periodo.Inicio, periodo.Fim);
        }
    }
}
