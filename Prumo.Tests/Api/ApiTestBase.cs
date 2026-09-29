using System.Net.Http.Json;
using System.Text.Json;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    public abstract class ApiTestBase : IClassFixture<ApiFactory>
    {
        protected readonly ApiFactory Factory;

        protected ApiTestBase(ApiFactory factory)
        {
            Factory = factory;
        }

        protected static async Task<string?> DetailAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
        }

        protected Task<User> UserAsync(params RoleName[] roles) =>
            Factory.CreateUserAsync($"{Guid.NewGuid():N}@prumo.dev", true, roles);

        protected async Task<Portfolio> PortfolioAsync(User owner, PortfolioStatus status = PortfolioStatus.Criado, params User[] members)
        {
            var portfolio = new Portfolio { Name = "Portfólio " + Guid.NewGuid().ToString("N")[..6], OwnerId = owner.Id, Status = status };
            portfolio.Members.Add(new PortfolioMember { PortfolioId = portfolio.Id, UserId = owner.Id });
            foreach (var member in members)
            {
                portfolio.Members.Add(new PortfolioMember { PortfolioId = portfolio.Id, UserId = member.Id });
            }

            await Factory.WithDbAsync(async db => { db.Portfolios.Add(portfolio); await db.SaveChangesAsync(); });
            return portfolio;
        }
    }
}
