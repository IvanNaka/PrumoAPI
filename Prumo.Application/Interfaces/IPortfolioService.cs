using Prumo.Application.DTOs.Portfolio;

namespace Prumo.Application.Interfaces
{
    public interface IPortfolioService
    {
        /// <summary>GET /portfolios — regra D11.</summary>
        Task<IEnumerable<PortfolioDto>> GetVisibleAsync();

        /// <summary>GET /portfolios/{id} — membro; inclui contagem de projetos e critérios (UC3).</summary>
        Task<PortfolioDto?> GetByIdAsync(Guid id);

        Task<PortfolioDto> CreateAsync(CreatePortfolioDto dto);
        Task<PortfolioDto> UpdateAsync(Guid id, CreatePortfolioDto dto);

        /// <summary>POST /portfolios/{id}/acoes/{acao} — aprovar / reavaliar / encerrar (3.4.2).</summary>
        Task<PortfolioDto> ApplyActionAsync(Guid id, string acao);

        Task<IEnumerable<PortfolioMemberDto>> GetMembersAsync(Guid id);
        Task<IEnumerable<PortfolioMemberDto>> AddMemberAsync(Guid id, Guid userId);
        Task RemoveMemberAsync(Guid id, Guid userId);

        /// <summary>Transições automáticas (3.4.2) chamadas pelos serviços de critérios, projetos e priorização.</summary>
        Task ApplyAutomaticEventAsync(Guid id, string evento);
    }
}
