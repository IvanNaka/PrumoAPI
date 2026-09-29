using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>
    /// F9 no portfólio (RF39): equipes com PortfolioId igual ao portfólio; se não houver, as equipes cujos
    /// membros são responsáveis pelas issues do portfólio. Mês: o da data final do período.
    /// </summary>
    public class CapacityIndicator : PortfolioIndicator<CapacityResult>
    {
        public CapacityIndicator(IndicatorDataLoader loader) : base(loader) { }

        public override string Nome => IndicatorNames.Capacidade;

        public override async Task<CapacityResult> Calcular(Guid portfolioId, Periodo periodo)
        {
            var teams = await Loader.PortfolioTeamsAsync(portfolioId);
            var equipes = new List<CapacityResult>();
            foreach (var team in teams)
            {
                equipes.Add(await Loader.CapacityAsync(team, periodo.Ate.Year, periodo.Ate.Month));
            }

            return CapacityCalculator.CalcularPortfolio(equipes, periodo.Ate.Year, periodo.Ate.Month);
        }
    }
}
