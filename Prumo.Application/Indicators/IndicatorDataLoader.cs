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

        /// <summary>Worklogs de cada projeto (horas e e-mail do autor). Alimentado pela integração Jira (T17).</summary>
        protected virtual Task<Dictionary<Guid, List<WorklogHours>>> WorklogHoursAsync(IReadOnlyCollection<Guid> projectIds) =>
            Task.FromResult(new Dictionary<Guid, List<WorklogHours>>());

        /// <summary>Custo/hora por e-mail dos membros das equipes (T15).</summary>
        protected virtual Task<IReadOnlyDictionary<string, decimal>> HourlyCostByEmailAsync() =>
            Task.FromResult<IReadOnlyDictionary<string, decimal>>(new Dictionary<string, decimal>());
    }
}
