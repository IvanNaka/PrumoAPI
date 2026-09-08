using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface ITeamRepository : IRepository<Team>
    {
        Task<Team?> GetByIdAsync(Guid id);
        Task<IEnumerable<Team>> GetAllAsync();
        Task<IEnumerable<Team>> GetByPortfolioIdAsync(Guid portfolioId);
        Task<Team?> GetByInviteCodeAsync(string inviteCode);
        Task<bool> InviteCodeExistsAsync(string inviteCode);
        Task<TeamUser?> GetMembershipAsync(Guid teamId, Guid userId);
        Task AddMemberAsync(TeamUser teamUser);
        Task RemoveMemberAsync(TeamUser teamUser);
    }
}
