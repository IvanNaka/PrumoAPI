using Prumo.Application.Indicators;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Formulas
{
    // T26 casos 1 e 2 — F1 (score) e F2 (ranking).
    public class PrioritizationCalculatorTests
    {
        private static readonly Guid Valor = Guid.NewGuid();
        private static readonly Guid Esforco = Guid.NewGuid();
        private static readonly Guid Risco = Guid.NewGuid();
        private static readonly Guid Alinhamento = Guid.NewGuid();

        private static readonly CriterionWeight[] Criterios =
        {
            new(Valor, 4, CriteriaType.Beneficio),
            new(Esforco, 2, CriteriaType.Custo),
            new(Risco, 2, CriteriaType.Custo),
            new(Alinhamento, 2, CriteriaType.Beneficio),
        };

        private static Dictionary<Guid, int> Notas(int valor, int esforco, int risco, int alinhamento) => new()
        {
            [Valor] = valor, [Esforco] = esforco, [Risco] = risco, [Alinhamento] = alinhamento,
        };

        [Fact]
        public void F1_Score_CasoDoPlano()
        {
            // n' = 5, 4, 3, 4; média = 42 / 10 = 4,2; score = (4,2 - 1) / 4 × 100 = 80,00
            var score = PrioritizationCalculator.Score(Criterios, Notas(5, 2, 3, 4));
            Assert.Equal(80.00m, score);
        }

        [Fact]
        public void F1_SemNotaEmTodosOsCriterios_NaoTemScore()
        {
            var notas = Notas(5, 2, 3, 4);
            notas.Remove(Risco);
            Assert.Null(PrioritizationCalculator.Score(Criterios, notas));
        }

        [Fact]
        public void F1_Extremos_De0A100()
        {
            Assert.Equal(100m, PrioritizationCalculator.Score(Criterios, Notas(5, 1, 1, 5)));
            Assert.Equal(0m, PrioritizationCalculator.Score(Criterios, Notas(1, 5, 5, 1)));
        }

        [Fact]
        public void F2_Empate_PrioridadeAltaFicaAFrente()
        {
            var media = new ProjectScores(Guid.NewGuid(), Priority.Media, new DateTime(2026, 1, 1), Notas(5, 2, 3, 4));
            var alta = new ProjectScores(Guid.NewGuid(), Priority.Alta, new DateTime(2026, 2, 1), Notas(5, 2, 3, 4));

            var ranking = PrioritizationCalculator.Rank(Criterios, new[] { media, alta });

            Assert.Equal(80m, ranking[0].Score);
            Assert.Equal(80m, ranking[1].Score);
            Assert.Equal(alta.ProjectId, ranking[0].ProjectId);
            Assert.Equal(1, ranking[0].Position);
            Assert.Equal(2, ranking[1].Position);
        }

        [Fact]
        public void F2_EmpateTotal_DataDeCriacaoMaisAntigaPrimeiro()
        {
            var novo = new ProjectScores(Guid.NewGuid(), Priority.Alta, new DateTime(2026, 3, 1), Notas(4, 2, 2, 4));
            var antigo = new ProjectScores(Guid.NewGuid(), Priority.Alta, new DateTime(2025, 3, 1), Notas(4, 2, 2, 4));
            var melhor = new ProjectScores(Guid.NewGuid(), Priority.Baixa, new DateTime(2026, 3, 1), Notas(5, 1, 1, 5));

            var ranking = PrioritizationCalculator.Rank(Criterios, new[] { novo, antigo, melhor });

            Assert.Equal(new[] { melhor.ProjectId, antigo.ProjectId, novo.ProjectId }, ranking.Select(r => r.ProjectId));
        }
    }
}
