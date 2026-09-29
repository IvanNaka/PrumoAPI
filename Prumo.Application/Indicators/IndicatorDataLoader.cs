using Microsoft.EntityFrameworkCore;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;

namespace Prumo.Application.Indicators
{
    /// <summary>
    /// Carrega do banco os dados que os indicadores precisam e chama os calculadores puros.
    /// </summary>
    public class IndicatorDataLoader
    {
        private readonly IAppDbContext _db;

        public IndicatorDataLoader(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>F5 para cada projeto informado (custo de horas + lançamentos).</summary>
        public async Task<Dictionary<Guid, BurnRateResult>> BurnRatesAsync(IReadOnlyCollection<Project> projects, DateOnly hoje)
        {
            var ids = projects.Select(p => p.Id).ToList();
            var expenses = await _db.BudgetExpenses.AsNoTracking()
                .Where(e => ids.Contains(e.ProjectId))
                .GroupBy(e => e.ProjectId)
                .Select(g => new { g.Key, Total = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(g => g.Key, g => g.Total);

            var worklogs = await WorklogHoursAsync(ids);
            var costs = await HourlyCostByEmailAsync();

            return projects.ToDictionary(
                p => p.Id,
                p => BurnRateCalculator.CalcularProjeto(
                    new BurnRateProject(p.Id, p.Name, p.Status, p.StartDate, p.EndDate, p.CompletedAt, p.ApprovedBudget),
                    expenses.GetValueOrDefault(p.Id),
                    worklogs.GetValueOrDefault(p.Id) ?? new List<WorklogHours>(),
                    costs,
                    hoje));
        }

        /// <summary>F6 para cada projeto, usando o custo realizado do F5.</summary>
        public async Task<Dictionary<Guid, VplResult>> VplsAsync(IReadOnlyCollection<Project> projects, IReadOnlyDictionary<Guid, BurnRateResult> burnRates)
        {
            var ids = projects.Select(p => p.Id).ToList();
            var cases = await _db.BusinessCases.AsNoTracking()
                .Include(b => b.Flows)
                .Where(b => ids.Contains(b.ProjectId))
                .ToDictionaryAsync(b => b.ProjectId);
            var returns = (await _db.RealizedReturns.AsNoTracking()
                    .Where(r => ids.Contains(r.ProjectId))
                    .Select(r => new { r.ProjectId, r.Date, r.Value })
                    .ToListAsync())
                .ToLookup(r => r.ProjectId, r => new RealizedReturnValue(r.Date, r.Value));

            return projects.ToDictionary(p => p.Id, p =>
            {
                cases.TryGetValue(p.Id, out var bc);
                return VplCalculator.Calcular(
                    p.Id,
                    p.Name,
                    bc != null,
                    bc?.InitialInvestment ?? 0,
                    bc?.AnnualDiscountRate ?? 0,
                    bc?.Flows.Select(f => new CashFlow(f.Month, f.Value)) ?? Enumerable.Empty<CashFlow>(),
                    burnRates.TryGetValue(p.Id, out var burn) ? burn.CustoRealizado : 0,
                    p.StartDate,
                    returns[p.Id]);
            });
        }

        /// <summary>Worklogs de cada projeto (horas e e-mail do autor). Alimentado pela integração Jira (T17).</summary>
        protected virtual Task<Dictionary<Guid, List<WorklogHours>>> WorklogHoursAsync(IReadOnlyCollection<Guid> projectIds) =>
            Task.FromResult(new Dictionary<Guid, List<WorklogHours>>());

        /// <summary>Custo/hora por e-mail dos membros das equipes (T15).</summary>
        protected virtual Task<IReadOnlyDictionary<string, decimal>> HourlyCostByEmailAsync() =>
            Task.FromResult<IReadOnlyDictionary<string, decimal>>(new Dictionary<string, decimal>());
    }
}
