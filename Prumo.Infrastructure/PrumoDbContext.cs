using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Infrastructure.Configurations;

namespace Plantonize.Plantao.Infrastructure
{
    public class PrumoDbContext : DbContext, IAppDbContext
    {
        public PrumoDbContext(DbContextOptions<PrumoDbContext> options, IConfiguration configuration) : base(options){}

        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();
        public DbSet<PortfolioMember> PortfolioMembers => Set<PortfolioMember>();
        public DbSet<PortfolioObjective> PortfolioObjectives => Set<PortfolioObjective>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<PriorityCriteria> PriorityCriterias => Set<PriorityCriteria>();
        public DbSet<Objective> Objectives => Set<Objective>();
        public DbSet<KeyResult> KeyResults => Set<KeyResult>();
        public DbSet<ProjectObjective> ProjectObjectives => Set<ProjectObjective>();
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<BudgetExpense> BudgetExpenses => Set<BudgetExpense>();
        public DbSet<BusinessCase> BusinessCases => Set<BusinessCase>();
        public DbSet<CashFlowForecast> CashFlowForecasts => Set<CashFlowForecast>();
        public DbSet<RealizedReturn> RealizedReturns => Set<RealizedReturn>();
        public DbSet<ProjectDependency> ProjectDependencies => Set<ProjectDependency>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamUser> TeamUsers => Set<TeamUser>();
        public DbSet<Integration> Integrations => Set<Integration>();
        public DbSet<IntegrationSyncLog> IntegrationSyncLogs => Set<IntegrationSyncLog>();
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
        public DbSet<ProjectEvaluation> ProjectEvaluations => Set<ProjectEvaluation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IConfigurationScan).Assembly);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Todos os enums são gravados como texto (Seção 3.1 do plano de conformidade).
            configurationBuilder.Properties<Enum>().HaveConversion<string>();
        }
    }
}
