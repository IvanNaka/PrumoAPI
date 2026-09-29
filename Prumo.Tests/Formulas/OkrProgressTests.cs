using Prumo.Application.Indicators;

namespace Prumo.Tests.Formulas
{
    // T26 caso 3 — F4.
    public class OkrProgressTests
    {
        [Fact]
        public void F4_CasoDoPlano()
        {
            // KR1: meta 100, atual 50 -> 50%. KR2: meta 10, atual 12 -> 100% (limitado). OKR = 75%.
            Assert.Equal(50m, OkrProgressCalculator.KeyResult(100, 50));
            Assert.Equal(100m, OkrProgressCalculator.KeyResult(10, 12));
            Assert.Equal(75m, OkrProgressCalculator.Objective(new[] { new KeyResultValue(100, 50), new KeyResultValue(10, 12) }));
        }
    }
}
