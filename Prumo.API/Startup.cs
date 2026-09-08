using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Plantonize.Plantao.Infrastructure;
using Prumo.API.Extensions;


namespace Prumo.API
{
    public class Startup
    {
        private const int BODY_LOG_LIMIT = 4096;
        public Startup(IConfiguration configuration)
        {
            DotNetEnv.Env.Load();
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables() 
                .Build();
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

            services.AddApplicationDependencies();

            services.AddAuthorization();

            services.AddDbContext<PrumoDbContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Allow enums to be sent/received as strings (e.g. "Jira") instead of only
                    // numeric indices, which is what the front-end sends.
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddHealthChecks();
            services.AddHttpContextAccessor();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()    // Allows any origin (e.g., http://localhost:3000)
                          .AllowAnyMethod()    // Allows any HTTP method (GET, POST, PUT, DELETE, etc.)
                          .AllowAnyHeader();   // Allows any headers
                });
            });
            var jwtKey = Configuration["Jwt:Key"];
            var jwtIssuer = Configuration["Jwt:Issuer"];
            var jwtAudience = Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                // Fail fast with a clear message at startup. Without this guard, an empty
                // Jwt:Key reaches SymmetricSecurityKey lazily (the first time JwtBearerOptions
                // are resolved, i.e. on the first incoming request) and throws an obscure
                // ArgumentException that surfaces as a generic 500 on every request, including
                // CORS preflights - which the browser then reports as a CORS/network error.
                throw new InvalidOperationException(
                    "Configuração ausente: 'Jwt:Key' não foi definido. Configure-o em appsettings, " +
                    "variável de ambiente (Jwt__Key) ou no arquivo .env antes de iniciar a API.");
            }

            var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie("Cookies");

            var googleClientId = Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = Configuration["Authentication:Google:ClientSecret"];

            // The actual Google login flow (POST /api/auth/google) validates the ID token
            // directly via Google.Apis.Auth using only Authentication:Google:ClientId - it does
            // NOT depend on this ASP.NET Core OAuth handler (used only for server-side redirect
            // challenges, which this API doesn't perform). Registering it anyway with a missing
            // ClientSecret would fail GoogleOptions validation on every request (any scheme that
            // implements IAuthenticationRequestHandler is initialized by the authentication
            // middleware to check its callback path), crashing the whole API. So only register
            // it when both values are actually configured.
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authenticationBuilder.AddGoogle("Google", options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }

            authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true
                };
            });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Prumo API", Version = "v1" });
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

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            RoleSeeder.SeedRolesAsync(app.ApplicationServices).GetAwaiter().GetResult();

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
