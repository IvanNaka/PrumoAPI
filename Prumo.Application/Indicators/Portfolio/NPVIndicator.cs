using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F6 — VPL esperado × realizado dos projetos ativos com business case (RF41).</summary>
    public class NPVIndicator : PortfolioIndicator<VplResult>
    {
        public NPVIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.Vpl;

        public override async Task<VplResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectsAsync(portfolioId);
            var burnRates = await Loader.BurnRatesAsync(projects, Hoje);
            var vpls = await Loader.VplsAsync(projects, burnRates);
            return VplCalculator.CalcularPortfolio(vpls.Values.ToList());
        }
    }
}
