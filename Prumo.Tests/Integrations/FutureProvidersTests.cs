using Prumo.Application.DTOs.Integration;
using Prumo.Application.Interfaces;
using Prumo.Application.Services;
using Prumo.Domain.Enums;
using Prumo.Infrastructure.Integrations;

namespace Prumo.Tests.Integrations
{
    // T24 — Azure DevOps, GitHub e Trello como trabalho futuro (D09).
    public class FutureProvidersTests
    {
        public static IEnumerable<object[]> Providers() => new[]
        {
            new object[] { new AzureDevOpsProvider(), IntegrationType.AzureDevOps },
            new object[] { new GitHubProvider(), IntegrationType.GitHub },
            new object[] { new TrelloProvider(), IntegrationType.Trello },
        };

        [Theory]
        [MemberData(nameof(Providers))]
        public async Task ImplementamOContrato_ELancamNotSupported(IIntegrationProvider provider, IntegrationType type)
        {
            var credenciais = new IntegrationCredentials("https://x", "a@b.com", "t");

            Assert.Equal(type, provider.Type);
            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => provider.TestConnectionAsync(credenciais));
            Assert.Equal("Integração planejada para versão futura.", ex.Message);
            await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetIssuesAsync(credenciais, "K"));
        }

        [Fact]
        public void Factory_ResolveCadaFerramentaPeloTipo()
        {
            var factory = new IntegrationProviderFactory(new IIntegrationProvider[] { new AzureDevOpsProvider(), new GitHubProvider(), new TrelloProvider() });

            Assert.IsType<GitHubProvider>(factory.GetProvider(IntegrationType.GitHub));
            Assert.Null(factory.GetProvider(IntegrationType.Jira));
        }
    }
}
