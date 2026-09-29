using Prumo.Application.DTOs.Team;
using Prumo.Application.Indicators;

namespace Prumo.Application.Interfaces
{
    // Equipes, membros e capacidade (RF29–RF32, RF39, UC13).
    public interface ITeamService
    {
        Task<IEnumerable<EquipeDto>> GetAllAsync();
        Task<EquipeDto> GetAsync(Guid id);
        Task<EquipeDto> CreateAsync(SalvarEquipeDto dto);
        Task<EquipeDto> UpdateAsync(Guid id, SalvarEquipeDto dto);
        Task DeleteAsync(Guid id);

        Task<IEnumerable<MembroEquipeDto>> GetMembersAsync(Guid teamId);
        Task<MembroEquipeDto> AddMemberAsync(Guid teamId, SalvarMembroDto dto);
        Task<MembroEquipeDto> UpdateMemberAsync(Guid teamId, Guid memberId, SalvarMembroDto dto);
        Task RemoveMemberAsync(Guid teamId, Guid memberId);

        /// <summary>GET /equipes/{id}/capacidade?mes=AAAA-MM — F9 da equipe e de cada membro.</summary>
        Task<CapacityResult> GetCapacityAsync(Guid teamId, string? mes);
    }
}
