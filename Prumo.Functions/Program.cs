using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Plantonize.Plantao.Infrastructure;
using Plantonize.Plantao.Infrastructure.Repositories;
using Prumo.Application.Interfaces;
using Prumo.Application.Services;
using Prumo.Domain.Interfaces;
using Prumo.Infrastructure.Integrations;

DotNetEnv.Env.Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Configuration.AddEnvironmentVariables();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Same PrumoDbContext used by Prumo.API, so the automatic sync routine reads/writes the same
// Integrations/ExternalData tables.
builder.Services.AddDbContext<PrumoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<IIntegrationProviderFactory, IntegrationProviderFactory>();

// Integration providers: one IIntegrationProvider per external tool (RF47-RF50), resolved at
// runtime by IIntegrationProviderFactory based on Integration.Type. Keep this list in sync with
// Prumo.API's DependencyInjection.cs registration.
builder.Services.AddHttpClient<IIntegrationProvider, JiraIntegrationProvider>();

builder.Build().Run();
