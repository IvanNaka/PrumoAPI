using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F7 — lead time das issues concluídas no período (RF40).</summary>
    public class LeadTimeIndicator : PortfolioIndicator<LeadTimeResult>
    {
        public LeadTimeIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.LeadTime;

        public override async Task<LeadTimeResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectsAsync(portfolioId);
            var issues = await Loader.IssuesAsync(projects.Select(p => p.Id).ToList());
            return CalcularLeadTimeMediano(issues.Values.SelectMany(i => i), periodo);
        }

        /// <summary>LeadTimeIndicator.calcularLeadTimeMediano (Figura 9).</summary>
        public static LeadTimeResult CalcularLeadTimeMediano(IEnumerable<IssueData> issues, Periodo periodo) =>
            LeadTimeCalculator.Calcular(issues, periodo.Inicio, periodo.Fim);
    }
}
