using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Plantonize.Plantao.Infrastructure;
using Prumo.Application.Indicators;
using Prumo.Application.Indicators.Models;
using Prumo.Application.Indicators.Portfolio;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Indicators
{
    // T18 — uma classe por indicador (Figura 9), usando os calculadores das Fórmulas (Seção 3.5).
    public class PortfolioIndicatorsTests
    {
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        private static readonly DateTime Agora = DateTime.UtcNow;

        private readonly PrumoDbContext _db;
        private readonly IndicatorDataLoader _loader;
        private readonly Guid _portfolioId;
        private readonly Periodo _periodo = Periodo.Criar(null, null);

        public PortfolioIndicatorsTests()
        {
            var options = new DbContextOptionsBuilder<PrumoDbContext>().UseInMemoryDatabase("indicadores-" + Guid.NewGuid()).Options;
            _db = new PrumoDbContext(options, new ConfigurationBuilder().Build());
            _loader = new IndicatorDataLoader(_db);

            var portfolio = new Portfolio { Name = "Portfólio", OwnerId = Guid.NewGuid(), Status = PortfolioStatus.Monitoramento };
            _portfolioId = portfolio.Id;

            // A: Run, 100 mil, alinhado a OKR, depende de B.  B: Grow, 50 mil, EmRisco.  C: cancelado (fora de tudo).
            var a = Projeto("A", StrategicCategory.Run, 100_000, ProjectStatus.EmAndamento);
            var b = Projeto("B", StrategicCategory.Grow, 50_000, ProjectStatus.EmRisco);
            var c = Projeto("C", StrategicCategory.Transform, 500_000, ProjectStatus.Cancelado);

            var okr = new Objective { Title = "Crescer" };
            okr.KeyResults.Add(new KeyResult { ObjectiveId = okr.Id, Title = "KR", TargetValue = 100, CurrentValue = 40 });

            _db.Portfolios.Add(portfolio);
            _db.Projects.AddRange(a, b, c);
            _db.Objectives.Add(okr);
            _db.PortfolioObjectives.Add(new PortfolioObjective { PortfolioId = portfolio.Id, ObjectiveId = okr.Id });
            _db.ProjectObjectives.Add(new ProjectObjective { ProjectId = a.Id, ObjectiveId = okr.Id });
            _db.ProjectDependencies.Add(new ProjectDependency { PortfolioId = portfolio.Id, ProjectId = a.Id, DependsOnProjectId = b.Id });
            _db.BudgetExpenses.Add(new BudgetExpense { ProjectId = a.Id, Description = "Licenças", Amount = 20_000, Date = Hoje.AddDays(-10) });

            // Issues de A: 2 entregas e 1 bug concluídos (lead time 4, 10 e 6 dias) e 1 tarefa aberta atrasada.
            _db.Issues.AddRange(
                Issue(a.Id, "A-1", ExternalIssueType.Story, done: true, leadTime: 4),
                Issue(a.Id, "A-2", ExternalIssueType.Task, done: true, leadTime: 10),
                Issue(a.Id, "A-3", ExternalIssueType.Bug, done: true, leadTime: 6),
                Issue(a.Id, "A-4", ExternalIssueType.Task, done: false, leadTime: 0),
                Issue(c.Id, "C-1", ExternalIssueType.Bug, done: true, leadTime: 50));

            var team = new Team { Name = "Squad", PortfolioId = portfolio.Id };
            team.Members.Add(new TeamUser { TeamId = team.Id, Name = "Dev", Email = "dev@empresa.com", HourlyCost = 100, MonthlyCapacityHours = 160 });
            _db.Teams.Add(team);
            _db.SaveChanges();
        }

        private Project Projeto(string nome, StrategicCategory categoria, decimal orcamento, ProjectStatus status) => new()
        {
            Name = nome, PortfolioId = _portfolioId, OwnerId = Guid.NewGuid(), Status = status, StrategicCategory = categoria,
            ApprovedBudget = orcamento, StartDate = Hoje.AddDays(-90), EndDate = Hoje.AddDays(180),
        };

        private static ExternalIssue Issue(Guid projectId, string key, ExternalIssueType type, bool done, int leadTime) => new()
        {
            ProjectId = projectId, ExternalId = key, Title = key, Type = type, Status = done ? "Concluído" : "Em andamento",
            Done = done, CreatedAt = Agora.AddDays(-20), CompletedAt = done ? Agora.AddDays(-20 + leadTime) : null,
            DueDate = done ? null : Hoje.AddDays(-1), EstimateHours = 8, SpentHours = 0, AssigneeEmail = "dev@empresa.com",
            UpdatedAt = Agora,
        };

        [Fact]
        public async Task BurnRate_SomaOsProjetosAtivos()
        {
            var r = await new BurnRateIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.True(r.Disponivel);
            Assert.Equal(150_000m, r.OrcamentoAprovado);
            Assert.Equal(20_000m, r.CustoRealizado);
            Assert.Equal(13.33m, r.PercentualConsumido);
            Assert.Equal(2, r.Projetos!.Count);
        }

        [Fact]
        public async Task Saude_FlagsDeRiscoDependenciaEAtraso()
        {
            var r = await new PortfolioHealthIndicator(_loader).Calcular(_portfolioId, _periodo);

            var a = r.Projetos.Single(p => p.Nome == "A");
            var b = r.Projetos.Single(p => p.Nome == "B");
            Assert.Equal(new[] { HealthCalculator.FlagAtrasadas, HealthCalculator.FlagDependencia }, a.Flags); // 1/4 atrasadas > 20%
            Assert.Equal(50, a.PontuacaoSaude);
            Assert.Equal(75m, a.Progresso);
            Assert.Equal(new[] { HealthCalculator.FlagEmRisco }, b.Flags);
            Assert.Equal(62.5m, r.Saude);
            Assert.Equal(HealthCalculator.Atencao, r.Classificacao);
        }

        [Fact]
        public async Task Alocacao_PercentualDoOrcamentoPorCategoria()
        {
            var r = await new StrategicAllocationIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.Equal(66.67m, r.Categorias.Single(c => c.Categoria == "Run").PercentualOrcamento);
            Assert.Equal(33.33m, r.Categorias.Single(c => c.Categoria == "Grow").PercentualOrcamento);
            Assert.Equal(0, r.Categorias.Single(c => c.Categoria == "Transform").Quantidade);
        }

        [Fact]
        public async Task Qualidade_BugsPorEntregaNoPeriodo()
        {
            var r = await new QualityIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.Equal(1, r.Bugs);
            Assert.Equal(2, r.Entregas);
            Assert.Equal(0.5m, r.RazaoBugsPorEntrega);
            Assert.Equal(66.67m, r.IndiceQualidade);
            Assert.False(r.AtingiuMeta);
        }

        [Fact]
        public async Task LeadTime_MedianaDasConcluidas()
        {
            var r = await new LeadTimeIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.Equal(3, r.Quantidade);
            Assert.Equal(6m, r.LeadTimeMediano);
            Assert.Equal(6.67m, r.LeadTimeMedio);
        }

        [Fact]
        public async Task LeadTime_ForaDoPeriodo_Indisponivel()
        {
            var r = await new LeadTimeIndicator(_loader).Calcular(_portfolioId, Periodo.Criar(Hoje.AddYears(-2), Hoje.AddYears(-1)));

            Assert.False(r.Disponivel);
            Assert.Equal("Dados insuficientes para calcular este indicador.", r.Motivo);
        }

        [Fact]
        public async Task Capacidade_EquipesVinculadasAoPortfolio()
        {
            var r = await new CapacityIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.True(r.Disponivel);
            Assert.Equal(160m, r.Capacidade);
            Assert.Equal(8m, r.Demanda);
            Assert.Equal(5m, r.Ocupacao);
            Assert.Equal("Subutilizada", r.Classificacao);
        }

        [Fact]
        public async Task Capacidade_SemEquipeVinculada_UsaResponsaveisDasIssues()
        {
            var team = await _db.Teams.SingleAsync();
            team.PortfolioId = null;
            await _db.SaveChangesAsync();

            var r = await new CapacityIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.Equal(160m, r.Capacidade);
        }

        [Fact]
        public async Task Vpl_SemBusinessCase_Indisponivel()
        {
            var r = await new NPVIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.False(r.Disponivel);
        }

        [Fact]
        public async Task AlinhamentoOkr_ProjetosEOrcamentoAlinhados()
        {
            var r = await new OkrAlignmentIndicator(_loader).Calcular(_portfolioId, _periodo);

            Assert.Equal(50m, r.PercentualProjetosAlinhados);
            Assert.Equal(66.67m, r.PercentualOrcamentoAlinhado);
            Assert.Equal("B", r.ListaDesalinhados.Single().Nome);
            Assert.Equal(40m, r.ProgressoPorOkr.Single().Progresso);
        }
    }
}
