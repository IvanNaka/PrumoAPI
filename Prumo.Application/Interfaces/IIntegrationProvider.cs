using Prumo.Application.DTOs.Integration;
using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// IIntegracaoProvider: contrato de cada ferramenta externa (Jira completo; Azure DevOps, GitHub
    /// e Trello ficam como trabalho futuro — D09). Resolvido por <see cref="IIntegrationProviderFactory"/>.
    /// </summary>
    public interface IIntegrationProvider
    {
        IntegrationType Type { get; }

        /// <summary>TestarConexao: true quando a ferramenta aceita as credenciais.</summary>
        Task<bool> TestConnectionAsync(IntegrationCredentials credentials, CancellationToken cancellationToken = default);
    }
}
