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

        /// <summary>F9 da equipe (e de cada membro) no mês informado.</summary>
        public async Task<CapacityResult> CapacityAsync(Team team, int year, int month)
        {
            var members = team.Members
                .Select(m => new MemberCapacity(m.Id, m.Name, m.Email, m.MonthlyCapacityHours))
                .ToList();
            var emails = members.Select(m => m.Email.ToLowerInvariant()).ToList();

            var issues = await OpenIssuesForEmailsAsync(emails);
            var worklogs = await WorklogsForEmailsAsync(emails, year, month);
            return CapacityCalculator.CalcularEquipe(team.Id, team.Name, members, issues, worklogs, year, month);
        }

        /// <summary>Worklogs de cada projeto (horas e e-mail do autor). Alimentado pela integração Jira (T17).</summary>
        protected virtual Task<Dictionary<Guid, List<WorklogHours>>> WorklogHoursAsync(IReadOnlyCollection<Guid> projectIds) =>
            Task.FromResult(new Dictionary<Guid, List<WorklogHours>>());

        /// <summary>Issues não concluídas atribuídas aos e-mails (demanda do F9). Alimentado pelo Jira (T17).</summary>
        protected virtual Task<List<OpenIssueDemand>> OpenIssuesForEmailsAsync(IReadOnlyCollection<string> emails) =>
            Task.FromResult(new List<OpenIssueDemand>());

        /// <summary>Worklogs do mês lançados pelos e-mails (utilização do F9). Alimentado pelo Jira (T17).</summary>
        protected virtual Task<List<MonthWorklog>> WorklogsForEmailsAsync(IReadOnlyCollection<string> emails, int year, int month) =>
            Task.FromResult(new List<MonthWorklog>());

        /// <summary>
        /// Custo/hora por e-mail dos membros das equipes (D13). Se o mesmo e-mail estiver em mais de uma
        /// equipe com custos diferentes, usa a média.
        /// </summary>
        protected virtual async Task<IReadOnlyDictionary<string, decimal>> HourlyCostByEmailAsync()
        {
            var members = await _db.TeamUsers.AsNoTracking()
                .Select(m => new { m.Email, m.HourlyCost })
                .ToListAsync();
            return members
                .GroupBy(m => m.Email.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Average(m => m.HourlyCost));
        }
    }
}
