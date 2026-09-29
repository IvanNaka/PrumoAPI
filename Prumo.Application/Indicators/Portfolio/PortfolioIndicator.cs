using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators.Portfolio
{
    /// <summary>Indicador do portfólio (Figura 9). <see cref="Nome"/> é a chave usada no dashboard.</summary>
    public interface IPortfolioIndicator
    {
        string Nome { get; }

        Task<IndicatorResult> CalcularAsync(Guid portfolioId, Periodo periodo);
    }

    public abstract class PortfolioIndicator<T> : IPortfolioIndicator where T : IndicatorResult
    {
        protected PortfolioIndicator(IndicatorDataLoader loader)
        {
            Loader = loader;
        }

        protected IndicatorDataLoader Loader { get; }

        public abstract string Nome { get; }

        public abstract Task<T> Calcular(Guid portfolioId, Periodo periodo);

        async Task<IndicatorResult> IPortfolioIndicator.CalcularAsync(Guid portfolioId, Periodo periodo) =>
            await Calcular(portfolioId, periodo);

        protected static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>Nomes (chaves) dos 8 indicadores do dashboard.</summary>
    public static class IndicatorNames
    {
        public const string BurnRate = "burnRate";
        public const string Saude = "saude";
        public const string AlocacaoEstrategica = "alocacaoEstrategica";
        public const string Qualidade = "qualidade";
        public const string Capacidade = "capacidade";
        public const string LeadTime = "leadTime";
        public const string Vpl = "vpl";
        public const string AlinhamentoOkr = "alinhamentoOkr";

        public static readonly string[] Todos =
        {
            BurnRate, Saude, AlocacaoEstrategica, Qualidade, Capacidade, LeadTime, Vpl, AlinhamentoOkr,
        };
    }
}
