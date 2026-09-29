using Prumo.Application.Common;
using Prumo.Application.Indicators;
using Prumo.Application.StateMachines;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Formulas
{
    /// <summary>
    /// T26 — casos obrigatórios das fórmulas (Seção 3.5), com os valores esperados do plano.
    /// Os demais testes de cada calculador ficam nos arquivos específicos desta pasta.
    /// </summary>
    public class CasosObrigatoriosTests
    {
        private static readonly DateOnly Hoje = new(2026, 9, 29);

        // 1. F1 — Score: Valor (B,4)=5, Esforço (C,2)=2, Risco (C,2)=3, Alinhamento (B,2)=4 -> 80,00.
        [Fact]
        public void F1_Score_80()
        {
            Guid valor = Guid.NewGuid(), esforco = Guid.NewGuid(), risco = Guid.NewGuid(), alinhamento = Guid.NewGuid();
            var criterios = new[]
            {
                new CriterionWeight(valor, 4, CriteriaType.Beneficio),
                new CriterionWeight(esforco, 2, CriteriaType.Custo),
                new CriterionWeight(risco, 2, CriteriaType.Custo),
                new CriterionWeight(alinhamento, 2, CriteriaType.Beneficio),
            };
            var notas = new Dictionary<Guid, int> { [valor] = 5, [esforco] = 2, [risco] = 3, [alinhamento] = 4 };

            Assert.Equal(80.00m, PrioritizationCalculator.Score(criterios, notas));
        }

        // 2. F2 — Empate em 80: prioridade Alta fica à frente de Media.
        [Fact]
        public void F2_Empate_PrioridadeAltaNaFrente()
        {
            var criterio = Guid.NewGuid();
            var criterios = new[] { new CriterionWeight(criterio, 1, CriteriaType.Beneficio) };
            var notas = new Dictionary<Guid, int> { [criterio] = 4 }; // (4-1)/4 × 100 = 75 para os dois
            var media = new ProjectScores(Guid.NewGuid(), Priority.Media, new DateTime(2026, 1, 1), notas);
            var alta = new ProjectScores(Guid.NewGuid(), Priority.Alta, new DateTime(2026, 2, 1), notas);

            var ranking = PrioritizationCalculator.Rank(criterios, new[] { media, alta });

            Assert.Equal(new[] { alta.ProjectId, media.ProjectId }, ranking.Select(r => r.ProjectId));
            Assert.Equal(new[] { 1, 2 }, ranking.Select(r => r.Position));
        }

        // 3. F4 — KR1 50/100 = 50%; KR2 12/10 = 100% (limitado); OKR = 75%.
        [Fact]
        public void F4_Okr_75()
        {
            Assert.Equal(50m, OkrProgressCalculator.KeyResult(100, 50));
            Assert.Equal(100m, OkrProgressCalculator.KeyResult(10, 12));
            Assert.Equal(75m, OkrProgressCalculator.Objective(new[] { new KeyResultValue(100, 50), new KeyResultValue(10, 12) }));
        }

        // 4. F5 — Orçamento 100 mil; lançamentos 20 mil; 100 h × R$ 100 = 30 mil; 3 de 9 meses.
        [Fact]
        public void F5_BurnRate()
        {
            var projeto = new BurnRateProject(Guid.NewGuid(), "P", ProjectStatus.EmAndamento, Hoje.AddDays(-90), Hoje.AddDays(180), null, 100_000);
            var custos = new Dictionary<string, decimal> { ["dev@prumo"] = 100 };

            var r = BurnRateCalculator.CalcularProjeto(projeto, 20_000, new[] { new WorklogHours("dev@prumo", 100) }, custos, Hoje);

            Assert.Equal(30_000m, r.CustoRealizado);
            Assert.Equal(10_000m, r.BurnRateMensal);
            Assert.Equal(30m, r.PercentualConsumido);
            Assert.Equal(90_000m, r.ProjecaoCustoTotal);
            Assert.False(r.RiscoEstouro);
        }

        // 5. F6 — 12,6825% a.a. (1% a.m.); investimento 1.000; fluxos 600 e 600 -> 182,24.
        [Fact]
        public void F6_Vpl_182_24()
        {
            var vpl = VplCalculator.Expected(1_000, 12.6825m, new[] { new CashFlow(1, 600), new CashFlow(2, 600) });

            Assert.Equal(182.24m, vpl);
        }

        // 6. F7 — durações 2, 4, 10 e 20 dias -> mediana 7, média 9.
        [Fact]
        public void F7_LeadTime()
        {
            var r = LeadTimeCalculator.FromDurations(new[] { 2.0, 4, 10, 20 });

            Assert.Equal(7m, r.LeadTimeMediano);
            Assert.Equal(9m, r.LeadTimeMedio);
        }

        // 7. F8 — 3 bugs e 12 entregas -> razão 0,25; índice 80%; meta não atingida.
        [Fact]
        public void F8_Qualidade()
        {
            var r = QualityCalculator.FromCounts(3, 12);

            Assert.Equal(0.25m, r.RazaoBugsPorEntrega);
            Assert.Equal(80m, r.IndiceQualidade);
            Assert.False(r.AtingiuMeta);
        }

        // 8. F9 — capacidade 160 h, demanda 200 h -> 125%, Sobrecarregada.
        [Fact]
        public void F9_Capacidade()
        {
            var membro = new MemberCapacity(Guid.NewGuid(), "Dev", "dev@prumo", 160);
            var r = CapacityCalculator.Calcular(new[] { membro }, new[] { new OpenIssueDemand("dev@prumo", 200, 0, false) },
                Array.Empty<MonthWorklog>(), 2026, 9);

            Assert.Equal(125m, r.Ocupacao);
            Assert.Equal("Sobrecarregada", r.Classificacao);
        }

        // 9. F12 — EmRisco e hoje > DataFim (2 flags) -> 50, Atenção.
        [Fact]
        public void F12_Saude()
        {
            var r = HealthCalculator.CalcularProjeto(
                new HealthProjectInput(Guid.NewGuid(), "P", ProjectStatus.EmRisco, Hoje.AddDays(-1), null, 0, 0, 0, false), Hoje);

            Assert.Equal(2, r.Flags.Count);
            Assert.Equal(50, r.PontuacaoSaude);
            Assert.Equal("Atenção", r.Classificacao);
        }

        // 10. F14 — com A->B e B->C: CriaCiclo(C, A) = true; CriaCiclo(A, C) = false.
        [Fact]
        public void F14_Ciclo()
        {
            Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
            var arestas = new[] { (a, b), (b, c) }.ToLookup(e => e.Item1, e => e.Item2);

            Assert.True(DependencyCycleDetector.CriaCiclo(c, a, arestas));
            Assert.False(DependencyCycleDetector.CriaCiclo(a, c, arestas));
        }

        // 11. Máquinas de estado: uma transição válida e uma inválida (RN22) para cada uma das 5.
        [Fact]
        public void MaquinasDeEstado_ValidaEInvalida()
        {
            Assert.Equal(PortfolioStatus.Configurado, PortfolioStateMachine.Aplicar(PortfolioStatus.Criado, PortfolioStateMachine.PrimeiroCriterio));
            AssertRn22(() => PortfolioStateMachine.Aplicar(PortfolioStatus.Criado, PortfolioStateMachine.Encerrar));

            Assert.Equal(ProjectStatus.EmAndamento, ProjectStateMachine.Aplicar(ProjectStatus.Planejado, "Iniciar"));
            AssertRn22(() => ProjectStateMachine.Aplicar(ProjectStatus.Cancelado, "Retomar"));

            Assert.Equal(EvaluationStatus.Aprovado, EvaluationStateMachine.Aplicar(EvaluationStatus.Priorizado, EvaluationStateMachine.Aprovar));
            AssertRn22(() => EvaluationStateMachine.Aplicar(EvaluationStatus.Avaliando, EvaluationStateMachine.Aprovar));

            Assert.Equal(IntegrationStatus.Sincronizando, IntegrationStateMachine.Aplicar(IntegrationStatus.Conectada, IntegrationStateMachine.Sincronizar));
            AssertRn22(() => IntegrationStateMachine.Aplicar(IntegrationStatus.NaoConfigurada, IntegrationStateMachine.Sincronizar));

            Assert.Equal(AlertStatus.Lida, AlertStateMachine.Aplicar(AlertStatus.Enviada, AlertStateMachine.MarcarLida));
            AssertRn22(() => AlertStateMachine.Aplicar(AlertStatus.Enviada, AlertStateMachine.Arquivar));
        }

        private static void AssertRn22(Action acao)
        {
            var ex = Assert.Throws<BusinessRuleException>(acao);
            Assert.Equal(409, ex.Status);
        }
    }
}
