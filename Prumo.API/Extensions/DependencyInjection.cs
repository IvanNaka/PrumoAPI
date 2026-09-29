using Plantonize.Plantao.Infrastructure;
using Plantonize.Plantao.Infrastructure.Repositories;
using Prumo.Application.Interfaces;
using Prumo.Application.Services;
using Prumo.Domain.Interfaces;
using Prumo.Infrastructure.Integrations;

namespace Prumo.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            // Domain-specific repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IPortfolioRepository,    PortfolioRepository>();
            services.AddScoped<IPriorityCriteriaRepository, PriorityCriteriaRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectEvaluationRepository, ProjectEvaluationRepository>();
            services.AddScoped<IProjectDependencyRepository, ProjectDependencyRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IBudgetRepository, BudgetRepository>();
            services.AddScoped<IBudgetExpenseRepository, BudgetExpenseRepository>();
            services.AddScoped<ITeamCapacityRepository, TeamCapacityRepository>();
            services.AddScoped<IRoadmapRepository, RoadmapRepository>();


            // Application infrastructure
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<PrumoDbContext>());
            services.AddScoped<ICurrentUserService, Prumo.API.Infrastructure.CurrentUserService>();
            services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

            // Register Services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<IPriorityCriteriaService, PriorityCriteriaService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectEvaluationService, ProjectEvaluationService>();
            services.AddScoped<IProjectDependencyService, ProjectDependencyService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IBudgetService, BudgetService>();
            services.AddScoped<ITeamCapacityService, TeamCapacityService>();
            services.AddScoped<IRoadmapService, RoadmapService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IIntegrationService, IntegrationService>();
            services.AddScoped<IIntegrationProviderFactory, IntegrationProviderFactory>();

            // Generic repository registration
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Integration providers: one IIntegrationProvider per external tool (RF47-RF50),
            // resolved at runtime by IIntegrationProviderFactory based on Integration.Type.
            services.AddHttpClient<IIntegrationProvider, JiraIntegrationProvider>();

            // RF51 automatic sync is handled by the Prumo.Functions Azure Functions project
            // (daily timer trigger + queue trigger), not by an in-process background service.

            return services;
        }
    }
}
