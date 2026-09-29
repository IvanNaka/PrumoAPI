using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Portfolio;
using Prumo.Application.Indicators;
using Prumo.Application.Indicators.Models;
using Prumo.Application.Indicators.Portfolio;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    /// <summary>Dashboard do portfólio (RF35–RF42, UC11): os 8 indicadores em paralelo.</summary>
    public class DashboardService : IDashboardService
    {
        private readonly IAppDbContext _db;
        private readonly IPortfolioAccessService _access;
        private readonly IServiceScopeFactory _scopeFactory;

        public DashboardService(IAppDbContext db, IPortfolioAccessService access, IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _access = access;
            _scopeFactory = scopeFactory;
        }

        public async Task<DashboardDto> GetAsync(Guid portfolioId, DateOnly? de, DateOnly? ate)
        {
            await _access.EnsureAccessAsync(portfolioId);
            var periodo = Periodo.Criar(de, ate);

            // Cada indicador roda no seu próprio scope (DbContext próprio), todos ao mesmo tempo.
            var tarefas = IndicatorNames.Todos.ToDictionary(nome => nome, nome => CalcularAsync(nome, portfolioId, periodo));
            await Task.WhenAll(tarefas.Values);
            T R<T>(string nome) where T : IndicatorResult => (T)tarefas[nome].Result;

            var ultimaSincronizacao = await _db.Integrations.AsNoTracking()
                .Where(i => i.Type == IntegrationType.Jira)
                .Select(i => i.LastSyncedAt)
                .FirstOrDefaultAsync();

            return new DashboardDto
            {
                De = periodo.De,
                Ate = periodo.Ate,
                UltimaSincronizacaoJira = ultimaSincronizacao,
                BurnRate = R<BurnRateResult>(IndicatorNames.BurnRate),
                Saude = R<HealthResult>(IndicatorNames.Saude),
                AlocacaoEstrategica = R<AllocationResult>(IndicatorNames.AlocacaoEstrategica),
                Qualidade = R<QualityResult>(IndicatorNames.Qualidade),
                Capacidade = R<CapacityResult>(IndicatorNames.Capacidade),
                LeadTime = R<LeadTimeResult>(IndicatorNames.LeadTime),
                Vpl = R<VplResult>(IndicatorNames.Vpl),
                AlinhamentoOkr = R<OkrAlignmentResult>(IndicatorNames.AlinhamentoOkr),
            };
        }

        public async Task<IndicatorResult> GetIndicatorAsync(Guid portfolioId, string nome, DateOnly? de, DateOnly? ate)
        {
            var chave = IndicatorNames.Todos.FirstOrDefault(n => string.Equals(n, nome, StringComparison.OrdinalIgnoreCase))
                ?? throw new BusinessRuleException(404, $"Indicador '{nome}' não existe. Use: {string.Join(", ", IndicatorNames.Todos)}.");
            await _access.EnsureAccessAsync(portfolioId);
            return await CalcularAsync(chave, portfolioId, Periodo.Criar(de, ate));
        }

        private async Task<IndicatorResult> CalcularAsync(string nome, Guid portfolioId, Periodo periodo)
        {
            using var scope = _scopeFactory.CreateScope();
            var indicator = scope.ServiceProvider.GetServices<IPortfolioIndicator>().Single(i => i.Nome == nome);
            return await indicator.CalcularAsync(portfolioId, periodo);
        }
    }
}
