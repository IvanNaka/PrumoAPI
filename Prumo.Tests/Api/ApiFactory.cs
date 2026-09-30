using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Plantonize.Plantao.Infrastructure;
using Prumo.API;
using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    /// <summary>
    /// Sobe a API em memória (TestServer) com banco InMemory e um validador de token do Google falso:
    /// o "ID token" do Google nos testes é o próprio e-mail, ou "invalido" / "offline".
    /// </summary>
    public class ApiFactory : WebApplicationFactory<Startup>
    {
        public const string JwtKey = "chave-de-testes-com-mais-de-32-caracteres!";
        private readonly string _databaseName = "prumo-tests-" + Guid.NewGuid();

        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(web => web.UseStartup<Startup>());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["Admin:Email"] = "",
            }));
            builder.ConfigureTestServices(services =>
            {
                foreach (var descriptor in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<PrumoDbContext>) ||
                             d.ServiceType == typeof(DbContextOptions) ||
                             (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                         .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<PrumoDbContext>(o => o.UseInMemoryDatabase(_databaseName));
                services.AddSingleton<IGoogleTokenValidator, FakeGoogleValidator>();

                // Jira falso: o token "token-valido" é aceito; qualquer outro é recusado.
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IIntegrationProvider)).ToList())
                {
                    services.Remove(descriptor);
                }
                // O job diário (07:00) não roda nos testes: as regras e a expiração são chamadas diretamente.
                foreach (var descriptor in services.Where(d => d.ImplementationType == typeof(Prumo.API.BackgroundServices.DailyNotificationJob)).ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<FakeJiraProvider>();
                services.AddSingleton<IIntegrationProvider>(sp => sp.GetRequiredService<FakeJiraProvider>());
            });
        }

        public async Task<T> WithDbAsync<T>(Func<PrumoDbContext, Task<T>> action)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();
            return await action(db);
        }

        public Task WithDbAsync(Func<PrumoDbContext, Task> action) =>
            WithDbAsync<object?>(async db => { await action(db); return null; });

        public async Task<User> CreateUserAsync(string email, bool active = true, params RoleName[] roles)
        {
            var user = new User { Name = email.Split('@')[0], Email = email.ToLowerInvariant(), IsActive = active };
            foreach (var role in roles)
            {
                user.Roles.Add(new UserRole { UserId = user.Id, Role = role });
            }

            await WithDbAsync(async db => { db.Users.Add(user); await db.SaveChangesAsync(); });
            return user;
        }

        /// <summary>Cliente HTTP autenticado com um JWT válido para o usuário.</summary>
        public HttpClient ClientFor(User user)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(user));
            return client;
        }

        /// <summary>
        /// JWT assinado com a mesma chave que a API está usando (variáveis de ambiente como Jwt__Key
        /// têm precedência na configuração da API).
        /// </summary>
        public string TokenFor(User user)
        {
            var key = Services.GetRequiredService<IConfiguration>()["Jwt:Key"] ?? JwtKey;
            return TokenFor(user, key);
        }

        public static string TokenFor(User user, string key)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
            };
            claims.AddRange(user.Roles.Select(r => new Claim("role", r.Role.ToString())));
            var token = new JwtSecurityToken("prumo-api", "prumo-web", claims, expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class FakeJiraProvider : IIntegrationProvider
        {
            public const string TokenValido = "token-valido";
            public IntegrationType Type => IntegrationType.Jira;
            public List<IntegrationCredentials> Testes { get; } = new();

            public Task<bool> TestConnectionAsync(IntegrationCredentials credentials, CancellationToken cancellationToken = default)
            {
                Testes.Add(credentials);
                return Task.FromResult(credentials.ApiToken == TokenValido);
            }

            /// <summary>Issues por chave de projeto e worklogs por chave de issue.</summary>
            public Dictionary<string, List<ExternalIssueData>> Issues { get; } = new();
            public Dictionary<string, List<ExternalWorklogData>> Worklogs { get; } = new();
            public Exception? Erro { get; set; }
            public List<string> WorklogsConsultados { get; } = new();

            public Task<IReadOnlyList<ExternalIssueData>> GetIssuesAsync(IntegrationCredentials credentials, string projectKey, CancellationToken cancellationToken = default)
            {
                if (Erro != null) throw Erro;
                if (credentials.ApiToken != TokenValido) throw new IntegrationAuthException("401");
                return Task.FromResult<IReadOnlyList<ExternalIssueData>>(Issues.GetValueOrDefault(projectKey) ?? new List<ExternalIssueData>());
            }

            public Task<IReadOnlyList<ExternalWorklogData>> GetWorklogsAsync(IntegrationCredentials credentials, string issueKey, CancellationToken cancellationToken = default)
            {
                WorklogsConsultados.Add(issueKey);
                return Task.FromResult<IReadOnlyList<ExternalWorklogData>>(Worklogs.GetValueOrDefault(issueKey) ?? new List<ExternalWorklogData>());
            }
        }

        private class FakeGoogleValidator : IGoogleTokenValidator
        {
            public Task<GoogleUserInfo> ValidateAsync(string idToken) => idToken switch
            {
                "invalido" => throw new InvalidJwtException("token inválido"),
                "offline" => throw new HttpRequestException("sem rede"),
                _ => Task.FromResult(new GoogleUserInfo(idToken, "Nome " + idToken.Split('@')[0])),
            };
        }
    }
}
