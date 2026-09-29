using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F12 — saúde do portfólio (RF35): média dos projetos Planejado, EmAndamento e EmRisco.</summary>
    public class PortfolioHealthIndicator : PortfolioIndicator<HealthResult>
    {
        public PortfolioHealthIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.Saude;

        public override async Task<HealthResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = (await Loader.ActiveProjectsAsync(portfolioId))
                .Where(p => HealthCalculator.Avaliavel(p.Status))
                .ToList();
            var inputs = await Loader.HealthInputsAsync(projects, Hoje);
            return HealthCalculator.CalcularPortfolio(inputs, Hoje);
        }
    }
}
