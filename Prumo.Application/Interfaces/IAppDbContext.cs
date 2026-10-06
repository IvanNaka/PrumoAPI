using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Abstração do DbContext usada pelos serviços da camada de aplicação.
    /// A implementação é o <c>PrumoDbContext</c> (Infrastructure).
    /// </summary>
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Portfolio> Portfolios { get; }
        DbSet<PortfolioMember> PortfolioMembers { get; }
        DbSet<PortfolioObjective> PortfolioObjectives { get; }
        DbSet<Project> Projects { get; }
        DbSet<PriorityCriteria> PriorityCriterias { get; }
        DbSet<Objective> Objectives { get; }
        DbSet<KeyResult> KeyResults { get; }
        DbSet<ProjectObjective> ProjectObjectives { get; }
        DbSet<Budget> Budgets { get; }
        DbSet<BudgetExpense> BudgetExpenses { get; }
        DbSet<BusinessCase> BusinessCases { get; }
        DbSet<CashFlowForecast> CashFlowForecasts { get; }
        DbSet<RealizedReturn> RealizedReturns { get; }
        DbSet<ProjectDependency> ProjectDependencies { get; }
        DbSet<Team> Teams { get; }
        DbSet<TeamUser> TeamUsers { get; }
        DbSet<ProjectTeam> ProjectTeams { get; }
        DbSet<Integration> Integrations { get; }
        DbSet<IntegrationSyncLog> IntegrationSyncLogs { get; }
        DbSet<ProjectEvaluation> ProjectEvaluations { get; }
        DbSet<ExternalIssue> Issues { get; }
        DbSet<ExternalWorklog> Worklogs { get; }
        DbSet<Alert> Alerts { get; }
        DbSet<Report> Reports { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Descarta as alterações pendentes (ChangeTracker.Clear).</summary>
        void DiscardChanges();
    }
}
