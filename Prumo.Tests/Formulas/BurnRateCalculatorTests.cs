using Prumo.Application.Indicators;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Formulas
{
    // T26 caso 4 — F5 (custo realizado e Burn Rate).
    public class BurnRateCalculatorTests
    {
        private static readonly DateOnly Hoje = new(2026, 6, 1);

        [Fact]
        public void F5_CasoDoPlano()
        {
            // Orçamento 100.000; lançamentos 20.000; worklogs de 100 h a R$ 100/h; início = hoje − 90 dias; fim = hoje + 180 dias.
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.EmAndamento, Hoje.AddDays(-90), Hoje.AddDays(180), null, 100_000m);
            var worklogs = new[] { new WorklogHours("dev@prumo.dev", 60m), new WorklogHours("DEV@prumo.dev", 40m) };
            var custos = new Dictionary<string, decimal> { ["dev@prumo.dev"] = 100m };

            var r = BurnRateCalculator.CalcularProjeto(projeto, 20_000m, worklogs, custos, Hoje);

            Assert.True(r.Disponivel);
            Assert.Equal(30_000m, r.CustoRealizado);
            Assert.Equal(10_000m, r.CustoHoras);
            Assert.Equal(3m, r.MesesDecorridos);
            Assert.Equal(9m, r.DuracaoMeses);
            Assert.Equal(10_000m, r.BurnRateMensal);
            Assert.Equal(30m, r.PercentualConsumido);
            Assert.Equal(90_000m, r.ProjecaoCustoTotal);
            Assert.Equal(70_000m, r.Saldo);
            Assert.False(r.RiscoEstouro);
            Assert.False(r.Estouro);
        }

        [Fact]
        public void F5_WorklogSemMembro_ContaEmHorasSemCusto()
        {
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.EmAndamento, Hoje.AddDays(-30), Hoje.AddDays(30), null, 1000m);
            var r = BurnRateCalculator.CalcularProjeto(projeto, 0, new[] { new WorklogHours("x@y", 5) }, new Dictionary<string, decimal>(), Hoje);
            Assert.Equal(5m, r.HorasSemCusto);
            Assert.Equal(0m, r.CustoRealizado);
        }

        [Fact]
        public void F5_OrcamentoZero_Indisponivel()
        {
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.EmAndamento, Hoje.AddDays(-30), Hoje.AddDays(30), null, 0m);
            var r = BurnRateCalculator.CalcularProjeto(projeto, 10, Array.Empty<WorklogHours>(), new Dictionary<string, decimal>(), Hoje);
            Assert.False(r.Disponivel);
            Assert.Equal("Dados insuficientes para calcular este indicador.", r.Motivo);
        }

        [Fact]
        public void F5_ProjetoQueAindaNaoComecou_BurnRateZero()
        {
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.Planejado, Hoje.AddDays(10), Hoje.AddDays(100), null, 1000m);
            var r = BurnRateCalculator.CalcularProjeto(projeto, 100, Array.Empty<WorklogHours>(), new Dictionary<string, decimal>(), Hoje);
            Assert.Equal(0m, r.BurnRateMensal);
        }

        [Fact]
        public void F5_Estouro()
        {
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.EmAndamento, Hoje.AddDays(-60), Hoje.AddDays(30), null, 1000m);
            var r = BurnRateCalculator.CalcularProjeto(projeto, 1500, Array.Empty<WorklogHours>(), new Dictionary<string, decimal>(), Hoje);
            Assert.True(r.Estouro);
            Assert.True(r.RiscoEstouro);
            Assert.Equal(150m, r.PercentualConsumido);
        }
    }
}
