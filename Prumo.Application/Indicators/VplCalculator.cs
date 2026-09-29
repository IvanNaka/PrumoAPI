using Prumo.Application.Indicators.Models;

namespace Prumo.Application.Indicators
{
    public record CashFlow(int Month, decimal Value);

    public record RealizedReturnValue(DateOnly Date, decimal Value);

    public class VplResult : IndicatorResult
    {
        public Guid? ProjetoId { get; set; }
        public string? ProjetoNome { get; set; }
        public decimal TaxaMensal { get; set; }
        public decimal VplEsperado { get; set; }
        public decimal VplRealizado { get; set; }
        public decimal Diferenca { get; set; }
        public decimal? PercentualAtingimento { get; set; }
        public List<VplResult>? Projetos { get; set; }
    }

    /// <summary>F6 — VPL esperado e realizado (RF25, RF41).</summary>
    public static class VplCalculator
    {
        /// <summary>i = (1 + TaxaDescontoAnual/100)^(1/12) - 1</summary>
        public static double MonthlyRate(decimal annualRatePercent) =>
            Math.Pow(1 + (double)annualRatePercent / 100.0, 1.0 / 12.0) - 1;

        /// <summary>VPL_esperado = -InvestimentoInicial + Σ_t [ FluxoPrevisto_t / (1+i)^t ]</summary>
        public static decimal Expected(decimal initialInvestment, decimal annualRatePercent, IEnumerable<CashFlow> flows)
        {
            var i = MonthlyRate(annualRatePercent);
            var presentValue = flows.Sum(f => (double)f.Value / Math.Pow(1 + i, f.Month));
            return IndicatorMath.R2(-(double)initialInvestment + presentValue);
        }

        /// <summary>
        /// VPL_realizado = -custoRealizado (F5) + Σ_r [ r.Valor / (1+i)^t_real(r) ],
        /// t_real(r) = max(1, meses inteiros entre Projeto.DataInicio e r.Data).
        /// </summary>
        public static decimal Realized(decimal realizedCost, decimal annualRatePercent, DateOnly projectStart, IEnumerable<RealizedReturnValue> returns)
        {
            var i = MonthlyRate(annualRatePercent);
            var presentValue = returns.Sum(r =>
            {
                var t = Math.Max(1, IndicatorMath.WholeMonthsBetween(projectStart, r.Date));
                return (double)r.Value / Math.Pow(1 + i, t);
            });
            return IndicatorMath.R2(-(double)realizedCost + presentValue);
        }

        public static VplResult Calcular(
            Guid projectId,
            string projectName,
            bool hasBusinessCase,
            decimal initialInvestment,
            decimal annualRatePercent,
            IEnumerable<CashFlow> flows,
            decimal realizedCost,
            DateOnly projectStart,
            IEnumerable<RealizedReturnValue> returns)
        {
            var result = new VplResult { ProjetoId = projectId, ProjetoNome = projectName };
            if (!hasBusinessCase)
            {
                // Projeto sem BusinessCase: disponivel = false.
                return result.Indisponivel<VplResult>();
            }

            var esperado = Expected(initialInvestment, annualRatePercent, flows);
            var realizado = Realized(realizedCost, annualRatePercent, projectStart, returns);

            result.TaxaMensal = IndicatorMath.R2(MonthlyRate(annualRatePercent) * 100);
            result.VplEsperado = esperado;
            result.VplRealizado = realizado;
            result.Diferenca = IndicatorMath.R2(realizado - esperado);
            // percentualAtingimento = VPL_realizado / VPL_esperado × 100 (só se VPL_esperado ≠ 0)
            result.PercentualAtingimento = esperado != 0 ? IndicatorMath.R2(realizado / esperado * 100m) : null;
            return result;
        }

        /// <summary>Portfólio: soma dos VPLs dos projetos que têm business case.</summary>
        public static VplResult CalcularPortfolio(IReadOnlyCollection<VplResult> projetos)
        {
            var disponiveis = projetos.Where(p => p.Disponivel).ToList();
            var result = new VplResult { Projetos = disponiveis };
            if (disponiveis.Count == 0)
            {
                return result.Indisponivel<VplResult>();
            }

            result.VplEsperado = IndicatorMath.R2(disponiveis.Sum(p => p.VplEsperado));
            result.VplRealizado = IndicatorMath.R2(disponiveis.Sum(p => p.VplRealizado));
            result.Diferenca = IndicatorMath.R2(result.VplRealizado - result.VplEsperado);
            result.PercentualAtingimento = result.VplEsperado != 0
                ? IndicatorMath.R2(result.VplRealizado / result.VplEsperado * 100m)
                : null;
            return result;
        }
    }
}
