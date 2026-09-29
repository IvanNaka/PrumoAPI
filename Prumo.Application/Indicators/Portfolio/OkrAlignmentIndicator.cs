using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F11 — alinhamento dos projetos ativos a OKRs (RF42).</summary>
    public class OkrAlignmentIndicator : PortfolioIndicator<OkrAlignmentResult>
    {
        public OkrAlignmentIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.AlinhamentoOkr;

        public override async Task<OkrAlignmentResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectBudgetsAsync(portfolioId);
            var objectives = await Loader.PortfolioObjectivesAsync(portfolioId);
            return OkrAlignmentCalculator.Calcular(projects, objectives);
        }
    }
}
