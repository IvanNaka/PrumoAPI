using Prumo.Application.Indicators;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Formulas
{
    // T26 caso 10 — F14 (ciclo) e F13 (dependência em risco).
    public class DependencyTests
    {
        [Fact]
        public void F14_Ciclo()
        {
            Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
            var arestas = new[] { (a, b), (b, c) }.ToLookup(e => e.Item1, e => e.Item2);

            Assert.True(DependencyCycleDetector.CriaCiclo(c, a, arestas));
            Assert.False(DependencyCycleDetector.CriaCiclo(a, c, arestas));
        }

        [Theory]
        [InlineData(ProjectStatus.EmRisco, "Projeto dependente está em risco")]
        [InlineData(ProjectStatus.Suspenso, "Projeto dependente está suspenso")]
        [InlineData(ProjectStatus.Cancelado, "Projeto dependente está cancelado")]
        public void F13_DestinoEmStatusDeRisco(ProjectStatus status, string motivo)
        {
            var origem = new DependencyProject(Guid.NewGuid(), "A", ProjectStatus.EmAndamento, new DateOnly(2026, 12, 31));
            var destino = new DependencyProject(Guid.NewGuid(), "B", status, new DateOnly(2026, 6, 30));

            var risco = DependencyRiskCalculator.Avaliar(origem, destino);

            Assert.True(risco.EmRisco);
            Assert.Equal(motivo, risco.Motivo);
        }

        [Fact]
        public void F13_DestinoTerminaDepois()
        {
            var origem = new DependencyProject(Guid.NewGuid(), "A", ProjectStatus.EmAndamento, new DateOnly(2026, 6, 30));
            var destino = new DependencyProject(Guid.NewGuid(), "B", ProjectStatus.EmAndamento, new DateOnly(2026, 12, 31));
            var concluido = destino with { Status = ProjectStatus.Concluido };

            Assert.Equal("Projeto dependente termina depois deste projeto", DependencyRiskCalculator.Avaliar(origem, destino).Motivo);
            Assert.False(DependencyRiskCalculator.Avaliar(origem, concluido).EmRisco);
        }
    }
}
