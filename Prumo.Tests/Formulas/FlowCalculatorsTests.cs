using Prumo.Application.Indicators;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Formulas
{
    // F7 (Lead Time) e F8 (Qualidade da entrega).
    public class FlowCalculatorsTests
    {
        private static readonly DateTime Base = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static IssueData Concluida(ExternalIssueType type, double dias) =>
            new(type, true, Base, Base.AddDays(dias), null, null, 0, null);

        [Fact]
        public void LeadTime_MedianaParEhMediaDosDoisDoMeio()
        {
            var issues = new[] { 2.0, 4, 6, 20 }.Select(d => Concluida(ExternalIssueType.Story, d));

            var r = LeadTimeCalculator.Calcular(issues, Base, Base.AddDays(60));

            Assert.Equal(5m, r.LeadTimeMediano);
            Assert.Equal(8m, r.LeadTimeMedio);
            Assert.Equal(4, r.Quantidade);
        }

        [Fact]
        public void LeadTime_IgnoraAbertasEForaDoPeriodo()
        {
            var issues = new[]
            {
                Concluida(ExternalIssueType.Task, 3),
                Concluida(ExternalIssueType.Task, 90),
                new IssueData(ExternalIssueType.Task, false, Base, null, null, null, 0, null),
            };

            var r = LeadTimeCalculator.Calcular(issues, Base, Base.AddDays(30));

            Assert.Equal(1, r.Quantidade);
            Assert.Equal(3m, r.LeadTimeMediano);
        }

        [Fact]
        public void Qualidade_RazaoIndiceEMeta()
        {
            var issues = Enumerable.Range(0, 10).Select(_ => Concluida(ExternalIssueType.Feature, 1))
                .Concat(new[] { Concluida(ExternalIssueType.Bug, 1), Concluida(ExternalIssueType.Bug, 1) });

            var r = QualityCalculator.Calcular(issues, Base, Base.AddDays(30));

            Assert.Equal(0.2m, r.RazaoBugsPorEntrega);
            Assert.Equal(83.33m, r.IndiceQualidade);
            Assert.True(r.AtingiuMeta);
            Assert.Equal(10, r.PorTipo["Feature"]);
        }

        [Fact]
        public void Qualidade_SemEntregas_Indisponivel()
        {
            var r = QualityCalculator.Calcular(new[] { Concluida(ExternalIssueType.Bug, 1) }, Base, Base.AddDays(30));

            Assert.False(r.Disponivel);
            Assert.Equal("Dados insuficientes para calcular este indicador.", r.Motivo);
        }
    }
}
