using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>F10 — alocação Run/Grow/Transform dos projetos ativos (RF37).</summary>
    public class StrategicAllocationIndicator : PortfolioIndicator<AllocationResult>
    {
        public StrategicAllocationIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.AlocacaoEstrategica;

        public override async Task<AllocationResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var projects = await Loader.ActiveProjectBudgetsAsync(portfolioId);
            return AllocationCalculator.Calcular(projects);
        }
    }
}
