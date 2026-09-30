using Prumo.Application.Indicators.Portfolio;
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

            // Indicadores do portfólio (Figura 9): um por chave do dashboard.
            services.AddScoped<IPortfolioIndicator, BurnRateIndicator>();
            services.AddScoped<IPortfolioIndicator, PortfolioHealthIndicator>();
            services.AddScoped<IPortfolioIndicator, StrategicAllocationIndicator>();
            services.AddScoped<IPortfolioIndicator, QualityIndicator>();
            services.AddScoped<IPortfolioIndicator, CapacityIndicator>();
            services.AddScoped<IPortfolioIndicator, LeadTimeIndicator>();
            services.AddScoped<IPortfolioIndicator, NPVIndicator>();
            services.AddScoped<IPortfolioIndicator, OkrAlignmentIndicator>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddSingleton<IReportRenderer, Prumo.Infrastructure.Reports.ReportRenderer>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProjectDependencyService, ProjectDependencyService>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IOnboardingService, OnboardingService>();
            services.AddScoped<IRoadmapService, RoadmapService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IIntegrationService, IntegrationService>();
            services.AddScoped<IIntegrationProviderFactory, IntegrationProviderFactory>();
            services.AddScoped<IIntegrationSyncService, IntegrationSyncService>();
            services.AddSingleton<Prumo.API.BackgroundServices.SyncTrigger>();
            services.AddSingleton<ISyncTrigger>(sp => sp.GetRequiredService<Prumo.API.BackgroundServices.SyncTrigger>());
            services.AddHostedService<Prumo.API.BackgroundServices.ScheduledSyncService>();

            // Notificações (RF45, RF46, Figura 30): serviço, regras, fila + dispatcher e job diário.
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<NotificationRulesService>();
            services.AddScoped<INotificationRulesService>(sp => sp.GetRequiredService<NotificationRulesService>());
            services.AddScoped<ISyncCompletedHandler>(sp => sp.GetRequiredService<NotificationRulesService>());
            services.AddScoped<IAdminNotifier, NotificationAdminNotifier>();
            services.AddSingleton<Prumo.API.BackgroundServices.NotificationQueue>();
            services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<Prumo.API.BackgroundServices.NotificationQueue>());
            services.AddHostedService<Prumo.API.BackgroundServices.NotificationDispatcher>();
            services.AddHostedService<Prumo.API.BackgroundServices.DailyNotificationJob>();

            // Generic repository registration
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Integration providers: one IIntegrationProvider per external tool (RF47-RF50),
            // resolved at runtime by IIntegrationProviderFactory based on Integration.Type.
            services.AddHttpClient<IIntegrationProvider, JiraIntegrationProvider>(c => c.Timeout = TimeSpan.FromSeconds(60));

            // RF48–RF50 (D09): só o contrato; lançam NotSupportedException e não aparecem na interface.
            services.AddSingleton<IIntegrationProvider, AzureDevOpsProvider>();
            services.AddSingleton<IIntegrationProvider, GitHubProvider>();
            services.AddSingleton<IIntegrationProvider, TrelloProvider>();

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
