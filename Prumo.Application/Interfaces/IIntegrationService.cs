using Prumo.Application.DTOs.Integration;

namespace Prumo.Application.Interfaces
{
    // Integração Jira (RF47, RF51, UC15, UC16).
    public interface IIntegrationService
    {
        Task<IntegracaoJiraDto> GetJiraAsync();

        /// <summary>PUT /integracoes/jira: salva e testa a conexão automaticamente (RN24 se falhar).</summary>
        Task<IntegracaoJiraDto> SaveJiraAsync(SalvarIntegracaoJiraDto dto);

        /// <summary>POST /integracoes/jira/testar.</summary>
        Task<IntegracaoJiraDto> TestJiraAsync();

        Task<IEnumerable<SincronizacaoLogDto>> GetJiraLogsAsync(int limit = 50);
    }
}
