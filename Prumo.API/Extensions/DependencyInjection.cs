using Plantonize.Plantao.Infrastructure;
using Plantonize.Plantao.Infrastructure.Repositories;
using Prumo.Application.Interfaces;
using Prumo.Application.Services;
using Prumo.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using Prumo.API.Security;
using Prumo.Infrastructure.Integrations;

namespace Prumo.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
        {
            // Domain-specific repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IPortfolioRepository,    PortfolioRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoadmapRepository, RoadmapRepository>();


            // Application infrastructure
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<PrumoDbContext>());
            services.AddScoped<ICurrentUserService, Prumo.API.Infrastructure.CurrentUserService>();
            services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
            services.AddScoped<IPortfolioAccessService, PortfolioAccessService>();

            // Register Services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<IPriorityCriteriaService, PriorityCriteriaService>();
            services.AddScoped<IPrioritizationService, PrioritizationService>();
            services.AddScoped<IOkrService, OkrService>();
            services.AddScoped<IFinanceService, FinanceService>();
            services.AddScoped<IProjectIndicatorsService, ProjectIndicatorsService>();
            services.AddScoped<Prumo.Application.Indicators.IndicatorDataLoader>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectDependencyService, ProjectDependencyService>();
            services.AddScoped<ITeamService, TeamService>();
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

            // Token do Jira criptografado com ASP.NET Data Protection; chaves guardadas no banco.
            services.AddDataProtection().SetApplicationName("Prumo");
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
                new ConfigureOptions<KeyManagementOptions>(o =>
                    o.XmlRepository = new DbXmlRepository(sp.GetRequiredService<IServiceScopeFactory>())));
            services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

            return services;
        }
    }
}
