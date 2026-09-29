using Prumo.Application.Indicators;

namespace Prumo.Tests.Formulas
{
    // T26 caso 8 — F9 (capacidade, ocupação e utilização).
    public class CapacityCalculatorTests
    {
        private static readonly MemberCapacity Ana = new(Guid.NewGuid(), "Ana", "ana@prumo.dev", 100);
        private static readonly MemberCapacity Bia = new(Guid.NewGuid(), "Bia", "bia@prumo.dev", 60);

        [Fact]
        public void F9_Capacidade160_Demanda200_Sobrecarregada()
        {
            var issues = new[]
            {
                new OpenIssueDemand("ana@prumo.dev", 150, 20, false),  // 130
                new OpenIssueDemand("BIA@prumo.dev", 80, 10, false),   // 70
                new OpenIssueDemand("bia@prumo.dev", 50, 0, true),     // concluída: fora
                new OpenIssueDemand("fora@prumo.dev", 99, 0, false),   // não é membro: fora
                new OpenIssueDemand("ana@prumo.dev", 5, 9, false),     // max(5-9, 0) = 0
            };

            var r = CapacityCalculator.Calcular(new[] { Ana, Bia }, issues, Array.Empty<MonthWorklog>(), 2026, 6);

            Assert.Equal(160m, r.Capacidade);
            Assert.Equal(200m, r.Demanda);
            Assert.Equal(125m, r.Ocupacao);
            Assert.Equal("Sobrecarregada", r.Classificacao);
        }

        [Fact]
        public void F9_UtilizacaoUsaSoOMesCorrente()
        {
            var worklogs = new[]
            {
                new MonthWorklog("ana@prumo.dev", new DateOnly(2026, 6, 3), 40),
                new MonthWorklog("ana@prumo.dev", new DateOnly(2026, 5, 30), 100),
            };

            var r = CapacityCalculator.Calcular(new[] { Ana }, Array.Empty<OpenIssueDemand>(), worklogs, 2026, 6);

            Assert.Equal(40m, r.HorasMes);
            Assert.Equal(40m, r.Utilizacao);
            Assert.Equal("Subutilizada", r.Classificacao);
        }

        [Theory]
        [InlineData(69.99, "Subutilizada")]
        [InlineData(70, "Adequada")]
        [InlineData(100, "Adequada")]
        [InlineData(100.01, "Sobrecarregada")]
        public void F9_Classificacao(decimal ocupacao, string esperado)
        {
            Assert.Equal(esperado, CapacityCalculator.Classificar(ocupacao));
        }

        [Fact]
        public void F9_SemMembros_Indisponivel()
        {
            var r = CapacityCalculator.CalcularEquipe(Guid.NewGuid(), "Vazia", Array.Empty<MemberCapacity>(), Array.Empty<OpenIssueDemand>(), Array.Empty<MonthWorklog>(), 2026, 6);
            Assert.False(r.Disponivel);
            Assert.Equal("Dados insuficientes para calcular este indicador.", r.Motivo);
        }
    }
}
