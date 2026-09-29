using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F5 no nível do portfólio (RF36): soma dos projetos ativos.</summary>
    public class BurnRateIndicator : PortfolioIndicator<BurnRateResult>
    {
        public BurnRateIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.BurnRate;

        public override async Task<BurnRateResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectsAsync(portfolioId);
            var burnRates = await Loader.BurnRatesAsync(projects, Hoje);
            return BurnRateCalculator.CalcularPortfolio(burnRates.Values.ToList());
        }
    }
}
