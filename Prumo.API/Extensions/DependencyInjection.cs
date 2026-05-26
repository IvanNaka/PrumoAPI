
using Plantonize.Plantao.Infrastructure.Repositories;
using Prumo.Application.Interfaces;
using Prumo.Application.Services;
using Prumo.Domain.Interfaces;

namespace Prumo.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            // Domain-specific repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IPortfolioRepository,    PortfolioRepository>();
            services.AddScoped<IPriorityCriteriaRepository, PriorityCriteriaRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IProjectEvaluationRepository, ProjectEvaluationRepository>();


            // Register Services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<IPriorityCriteriaService, PriorityCriteriaService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectEvaluationService, ProjectEvaluationService>();

            // Generic repository registration
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            return services;
        }
    }
}
