using Microsoft.EntityFrameworkCore;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

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

        /// <summary>"Projetos ativos" = status diferente de Cancelado e de Arquivado (Seção 3.5).</summary>
        public async Task<List<Project>> ActiveProjectsAsync(Guid portfolioId)
        {
            return await _db.Projects.AsNoTracking()
                .Where(p => p.PortfolioId == portfolioId
                            && p.Status != ProjectStatus.Cancelado
                            && p.Status != ProjectStatus.Arquivado)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>Projetos ativos com orçamento, categoria e OKRs associados (F10, F11).</summary>
        public async Task<List<ProjectBudget>> ActiveProjectBudgetsAsync(Guid portfolioId)
        {
            var projects = await ActiveProjectsAsync(portfolioId);
            var ids = projects.Select(p => p.Id).ToList();
            var okrs = (await _db.ProjectObjectives.AsNoTracking()
                    .Where(po => ids.Contains(po.ProjectId))
                    .Select(po => new { po.ProjectId, po.ObjectiveId })
                    .ToListAsync())
                .ToLookup(po => po.ProjectId, po => po.ObjectiveId);

            return projects
                .Select(p => new ProjectBudget(p.Id, p.Name, p.StrategicCategory, p.ApprovedBudget, okrs[p.Id].Distinct().ToList()))
                .ToList();
        }

        /// <summary>OKRs associados ao portfólio, com os KRs (F4 dentro do F11).</summary>
        public async Task<List<ObjectiveProgressInput>> PortfolioObjectivesAsync(Guid portfolioId)
        {
            var objectives = await _db.Objectives.AsNoTracking()
                .Include(o => o.KeyResults)
                .Where(o => _db.PortfolioObjectives.Any(po => po.PortfolioId == portfolioId && po.ObjectiveId == o.Id))
                .OrderBy(o => o.Title)
                .ToListAsync();

            return objectives
                .Select(o => new ObjectiveProgressInput(o.Id, o.Title,
                    o.KeyResults.Select(k => new KeyResultValue(k.TargetValue, k.CurrentValue)).ToList()))
                .ToList();
        }

        /// <summary>Issues de cada projeto (F7, F8, F12).</summary>
        public virtual async Task<Dictionary<Guid, List<IssueData>>> IssuesAsync(IReadOnlyCollection<Guid> projectIds)
        {
            var rows = await _db.Issues.AsNoTracking()
                .Where(i => projectIds.Contains(i.ProjectId))
                .Select(i => new
                {
                    i.ProjectId,
                    Data = new IssueData(i.Type, i.Done, i.CreatedAt, i.CompletedAt, i.DueDate, i.EstimateHours, i.SpentHours, i.AssigneeEmail),
                })
                .ToListAsync();
            return rows
                .GroupBy(r => r.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Data).ToList());
        }

        /// <summary>Dados do F12 para cada projeto: F5 (% consumido), issues e dependências em risco (F13).</summary>
        public async Task<List<HealthProjectInput>> HealthInputsAsync(IReadOnlyCollection<Project> projects, DateOnly hoje)
        {
            var ids = projects.Select(p => p.Id).ToList();
            var burnRates = await BurnRatesAsync(projects, hoje);
            var issues = await IssuesAsync(ids);
            var atRisk = await ProjectsWithDependencyAtRiskAsync(ids);

            return projects
                .Select(p =>
                {
                    var list = issues.GetValueOrDefault(p.Id) ?? new List<IssueData>();
                    return new HealthProjectInput(
                        p.Id,
                        p.Name,
                        p.Status,
                        p.EndDate,
                        burnRates.TryGetValue(p.Id, out var burn) ? burn.PercentualConsumido : null,
                        list.Count,
                        list.Count(i => i.Done),
                        list.Count(i => !i.Done && i.DueDate.HasValue && i.DueDate.Value < hoje),
                        atRisk.Contains(p.Id));
                })
                .ToList();
        }

        /// <summary>Projetos (origem) que dependem de pelo menos um projeto em risco (F13).</summary>
        public async Task<HashSet<Guid>> ProjectsWithDependencyAtRiskAsync(IReadOnlyCollection<Guid> projectIds)
        {
            var dependencies = await _db.ProjectDependencies.AsNoTracking()
                .Where(d => projectIds.Contains(d.ProjectId))
                .Select(d => new
                {
                    Origem = new DependencyProject(d.Project.Id, d.Project.Name, d.Project.Status, d.Project.EndDate),
                    Destino = new DependencyProject(d.DependsOnProject.Id, d.DependsOnProject.Name, d.DependsOnProject.Status, d.DependsOnProject.EndDate),
                })
                .ToListAsync();

            return dependencies
                .Where(d => DependencyRiskCalculator.Avaliar(d.Origem, d.Destino).EmRisco)
                .Select(d => d.Origem.Id)
                .ToHashSet();
        }

        /// <summary>
        /// Equipes do portfólio (CapacityIndicator): as vinculadas pelo PortfolioId; se nenhuma estiver
        /// vinculada, as equipes cujos membros aparecem como responsáveis das issues do portfólio.
        /// </summary>
        public async Task<List<Team>> PortfolioTeamsAsync(Guid portfolioId)
        {
            var linked = await _db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .Where(t => t.PortfolioId == portfolioId)
                .OrderBy(t => t.Name)
                .ToListAsync();
            if (linked.Count > 0)
            {
                return linked;
            }

            var assignees = await _db.Issues.AsNoTracking()
                .Where(i => i.Project.PortfolioId == portfolioId && i.AssigneeEmail != null)
                .Select(i => i.AssigneeEmail!)
                .Distinct()
                .ToListAsync();
            if (assignees.Count == 0)
            {
                return linked;
            }

            return await _db.Teams.AsNoTracking()
                .Include(t => t.Members)
                .Where(t => t.Members.Any(m => assignees.Contains(m.Email)))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>Worklogs de cada projeto (horas e e-mail do autor), vindos do Jira (T17).</summary>
        protected virtual async Task<Dictionary<Guid, List<WorklogHours>>> WorklogHoursAsync(IReadOnlyCollection<Guid> projectIds)
        {
            var rows = await _db.Worklogs.AsNoTracking()
                .Where(w => projectIds.Contains(w.Issue.ProjectId))
                .Select(w => new { w.Issue.ProjectId, w.AuthorEmail, w.Hours })
                .ToListAsync();
            return rows
                .GroupBy(r => r.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(r => new WorklogHours(r.AuthorEmail, r.Hours)).ToList());
        }

        /// <summary>Issues não concluídas atribuídas aos e-mails (demanda do F9).</summary>
        protected virtual async Task<List<OpenIssueDemand>> OpenIssuesForEmailsAsync(IReadOnlyCollection<string> emails)
        {
            return await _db.Issues.AsNoTracking()
                .Where(i => !i.Done && i.AssigneeEmail != null && emails.Contains(i.AssigneeEmail))
                .Select(i => new OpenIssueDemand(i.AssigneeEmail, i.EstimateHours, i.SpentHours, i.Done))
                .ToListAsync();
        }

        /// <summary>Worklogs do mês lançados pelos e-mails (utilização do F9).</summary>
        protected virtual async Task<List<MonthWorklog>> WorklogsForEmailsAsync(IReadOnlyCollection<string> emails, int year, int month)
        {
            var inicio = new DateOnly(year, month, 1);
            var fim = inicio.AddMonths(1);
            return await _db.Worklogs.AsNoTracking()
                .Where(w => w.AuthorEmail != null && emails.Contains(w.AuthorEmail) && w.Date >= inicio && w.Date < fim)
                .Select(w => new MonthWorklog(w.AuthorEmail, w.Date, w.Hours))
                .ToListAsync();
        }

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
