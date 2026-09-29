using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Plantonize.Plantao.Infrastructure;
using Prumo.API.Extensions;
using Prumo.API.Infrastructure;
using Prumo.Application.Common;
using Prumo.Application.Services;
using System.Text;
using System.Text.Json.Serialization;

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
                // O corpo das requisições não é registrado: ele pode conter o token do Jira (T16).
                httpLogging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponseStatusCode;
                httpLogging.RequestHeaders.Remove("Authorization");
                httpLogging.RequestBodyLogLimit = BODY_LOG_LIMIT;
                httpLogging.ResponseBodyLogLimit = BODY_LOG_LIMIT;
            });

            services.AddSingleton(Configuration);
            services.AddApplicationDependencies();

            services.AddAuthorization();

            services.AddDbContext<PrumoDbContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddProblemDetails();
            services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Enums trafegam como texto, com os mesmos valores do banco (Seção 3.1).
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    // RN04: campo obrigatório vazio / corpo inválido -> 400 com a lista em "errors".
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problem = new ValidationProblemDetails(context.ModelState)
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = ProblemTitles.For(400),
                            Detail = Messages.RN04_CamposObrigatorios,
                        };
                        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
                    };
                });
            services.AddHealthChecks();
            services.AddHttpContextAccessor();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithExposedHeaders("Content-Disposition");
                });
            });

            var jwtKey = Configuration["Jwt:Key"];
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

            // D01: o login é feito pelo Google no front-end; o back-end valida o ID token em
            // POST /api/auth/google e emite o próprio JWT, validado aqui.
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = JwtSettings.Issuer(Configuration),
                        ValidateAudience = true,
                        ValidAudience = JwtSettings.Audience(Configuration),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            // 401 também no formato ProblemDetails (RN01).
                            context.HandleResponse();
                            await ProblemWriter.WriteAsync(context.HttpContext, new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = ProblemTitles.For(401),
                                Detail = Messages.RN01_TokenInvalido,
                            });
                        },
                    };
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Prumo API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        Array.Empty<string>()
                    },
                });
            });
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler();
            app.UseRouting();
            app.UseHttpLogging();
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            AdminSeeder.SeedAsync(app.ApplicationServices).GetAwaiter().GetResult();

            app.UseHealthChecks("/");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
