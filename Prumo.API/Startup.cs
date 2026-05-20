using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Plantonize.Plantao.Infrastructure;


namespace Prumo.API
{
    public class Startup
    {
        private const int BODY_LOG_LIMIT = 4096;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpLogging(httpLogging =>
            {
                httpLogging.LoggingFields = HttpLoggingFields.All;
                httpLogging.RequestHeaders.Add("Request-Header-Demo");
                httpLogging.ResponseHeaders.Add("Response-Header-Demo");
                httpLogging.MediaTypeOptions.
                AddText("application/javascript");
                httpLogging.RequestBodyLogLimit = BODY_LOG_LIMIT;
                httpLogging.ResponseBodyLogLimit = BODY_LOG_LIMIT;
            });



            services.AddAuthorization();

            services.AddDbContext<PrumoDbContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddControllers();
            services.AddHealthChecks();
            services.AddHttpContextAccessor();
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FleetManager API", Version = "v1" });
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("https://login.microsoftonline.com/5f3d3de5-5d5f-45b9-bccf-6b8d5aea1a03/oauth2/v2.0/authorize"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID Connect scope" },
                                { "profile", "Profile scope" },
                                { $"{Configuration["AzureAd:ClientId"]}/.default", "Access FleetManager API" }
                            }
                        }
                    }
                });
            });
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseHttpLogging();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseHealthChecks("/");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FleetManager API v1");
                c.OAuthScopeSeparator(" ");
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
