using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prumo.API.Extensions;
using Prumo.Domain.Enums;

namespace Prumo.Tests.Api
{
    /// <summary>API com PRUMO_SEED_VALIDACAO=true.</summary>
    public class SeedApiFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PRUMO_SEED_VALIDACAO"] = "true",
                ["SeedValidacao:Emails:Validacao"] = "Validacao@Prumo.dev",
            }));
        }
    }

    // T25 — seed de validação (Cap. 4, seção 4.3).
    public class SeedValidacaoTests : IClassFixture<SeedApiFactory>
    {
        private readonly SeedApiFactory _factory;

        public SeedValidacaoTests(SeedApiFactory factory)
        {
            _factory = factory;
            _ = factory.Server; // sobe a API (o seed roda no Configure)
        }

        [Fact]
        public async Task Seed_CriaOsDadosDoRoteiro_EhIdempotente()
        {
            await SeedValidacao.SeedAsync(_factory.Services); // segunda execução não duplica

            await _factory.WithDbAsync(async db =>
            {
                Assert.Equal(7, await db.Users.CountAsync());
                var validacao = await db.Users.Include(u => u.Roles).SingleAsync(u => u.Email == "validacao@prumo.dev");
                Assert.Equal(Enum.GetValues<RoleName>().Length, validacao.Roles.Count);
                Assert.Equal(2, await db.Portfolios.CountAsync());
                Assert.Equal(14, await db.PortfolioMembers.CountAsync());
                Assert.Equal(8, await db.PriorityCriterias.CountAsync());
                Assert.Equal(12, await db.Projects.CountAsync());
                Assert.Contains(await db.Projects.Select(p => p.Status).ToListAsync(), s => s == ProjectStatus.EmRisco);
                Assert.Contains(await db.Projects.Select(p => p.Status).ToListAsync(), s => s == ProjectStatus.Suspenso);
                Assert.Equal(10, await db.Projects.CountAsync(p => p.CurrentScore != null));
                Assert.Equal(1, await db.Projects.CountAsync(p => !db.ProjectObjectives.Any(po => po.ProjectId == p.Id)));
                Assert.Equal(3, await db.Objectives.CountAsync());
                Assert.Equal(6, await db.KeyResults.CountAsync());
                Assert.Equal(12, await db.BudgetExpenses.Select(e => e.ProjectId).Distinct().CountAsync());
                Assert.True(await db.BusinessCases.CountAsync() >= 4);
                Assert.True(await db.RealizedReturns.Select(r => r.ProjectId).Distinct().CountAsync() >= 4);
                Assert.Equal(2, await db.Teams.CountAsync());
                Assert.Equal(6, await db.TeamUsers.CountAsync());
                Assert.Equal(4, await db.Issues.Where(i => i.Source == "Seed").Select(i => i.Type).Distinct().CountAsync());
                Assert.True(await db.Worklogs.CountAsync() > 0);

                // Notificações dos 5 tipos (T21).
                var tipos = await db.Alerts.Select(a => a.Type).Distinct().ToListAsync();
                Assert.Equal(Enum.GetValues<AlertType>().OrderBy(t => t), tipos.OrderBy(t => t));
            });

            // Os 8 indicadores do dashboard ficam disponíveis com os dados de seed.
            var usuario = await _factory.WithDbAsync(db => db.Users.Include(u => u.Roles).SingleAsync(u => u.Email == "validacao@prumo.dev"));
            var dashboard = await _factory.ClientFor(usuario).GetFromJsonAsync<JsonElement>($"/api/portfolios/{SeedValidacao.Portfolio1Id}/dashboard");
            foreach (var chave in new[] { "burnRate", "saude", "alocacaoEstrategica", "qualidade", "capacidade", "leadTime", "vpl", "alinhamentoOkr" })
            {
                Assert.True(dashboard.GetProperty(chave).GetProperty("disponivel").GetBoolean(), chave);
            }

            Assert.Equal("Sobrecarregada", dashboard.GetProperty("capacidade").GetProperty("equipes")[0].GetProperty("classificacao").GetString());
        }
    }
}
