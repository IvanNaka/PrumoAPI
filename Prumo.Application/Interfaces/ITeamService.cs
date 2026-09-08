using Prumo.Application.DTOs.Team;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    public interface ITeamService
    {
        Task<TeamDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<TeamDto>> GetAllAsync();
        Task<IEnumerable<TeamDto>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<TeamDto> CreateAsync(CreateTeamDto dto);
        Task UpdateAsync(UpdateTeamDto dto);
        Task DeleteAsync(Guid id);

        Task<TeamDto?> AddMemberAsync(Guid teamId, AddTeamMemberDto dto);
        Task<TeamDto?> RemoveMemberAsync(Guid teamId, Guid userId);

        /// <summary>
        /// Adds the given user to the team identified by its invite code (self-service join,
        /// as opposed to <see cref="AddMemberAsync"/> which requires the team owner/manager).
        /// </summary>
        Task<TeamDto> JoinAsync(Guid userId, string inviteCode);

        /// <summary>
        /// Returns the invite code for the given team so its owner can share it with others.
        /// </summary>
        Task<string?> GetInviteCodeAsync(Guid teamId);
    }
}
