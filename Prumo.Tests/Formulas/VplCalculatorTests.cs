using Prumo.Application.Indicators;

namespace Prumo.Tests.Formulas
{
    // T26 caso 5 — F6 (VPL).
    public class VplCalculatorTests
    {
        [Fact]
        public void F6_VplEsperado_CasoDoPlano()
        {
            // Taxa anual 12,6825% (≈ 1% ao mês); investimento 1.000; fluxos: mês 1 = 600, mês 2 = 600.
            var vpl = VplCalculator.Expected(1000m, 12.6825m, new[] { new CashFlow(1, 600m), new CashFlow(2, 600m) });
            Assert.Equal(182.24m, vpl);
        }

        [Fact]
        public void F6_TaxaMensalEquivalente()
        {
            Assert.Equal(0.01, VplCalculator.MonthlyRate(12.6825m), 6);
        }

        [Fact]
        public void F6_VplRealizado_UsaMesesInteirosDesdeOInicio()
        {
            var inicio = new DateOnly(2026, 1, 15);
            // retorno em 20/02 -> 1 mês inteiro; retorno em 10/01 -> max(1, 0) = 1
            var vpl = VplCalculator.Realized(1000m, 12.6825m, inicio, new[]
            {
                new RealizedReturnValue(new DateOnly(2026, 2, 20), 606m),
                new RealizedReturnValue(new DateOnly(2026, 1, 10), 505m),
            });
            Assert.Equal(100.00m, vpl); // -1000 + 600 + 500
        }

        [Fact]
        public void F6_SemBusinessCase_Indisponivel()
        {
            var r = VplCalculator.Calcular(Guid.NewGuid(), "P", false, 0, 0, Array.Empty<CashFlow>(), 0, new DateOnly(2026, 1, 1), Array.Empty<RealizedReturnValue>());
            Assert.False(r.Disponivel);
        }

        [Fact]
        public void F6_PercentualDeAtingimento()
        {
            var r = VplCalculator.Calcular(Guid.NewGuid(), "P", true, 1000m, 12.6825m,
                new[] { new CashFlow(1, 600m), new CashFlow(2, 600m) }, 1000m, new DateOnly(2026, 1, 1),
                new[] { new RealizedReturnValue(new DateOnly(2026, 2, 1), 606m), new RealizedReturnValue(new DateOnly(2026, 3, 1), 612.06m) });
            Assert.Equal(182.24m, r.VplEsperado);
            Assert.Equal(200.00m, r.VplRealizado);
            Assert.Equal(17.76m, r.Diferenca);
            Assert.Equal(109.75m, r.PercentualAtingimento);
        }
    }
}
