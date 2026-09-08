using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class TeamRepository : Repository<Team>, ITeamRepository
    {
        public TeamRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<Team?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(t => t.Portfolio)
                .Include(t => t.OwnerUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Team>> GetAllAsync()
        {
            return await _dbSet
                .Include(t => t.Portfolio)
                .Include(t => t.OwnerUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetByPortfolioIdAsync(Guid portfolioId)
        {
            return await _dbSet
                .Where(t => t.PortfolioId == portfolioId)
                .Include(t => t.Portfolio)
                .Include(t => t.OwnerUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .ToListAsync();
        }

        public async Task<Team?> GetByInviteCodeAsync(string inviteCode)
        {
            var normalizedCode = inviteCode?.Trim().ToUpperInvariant() ?? string.Empty;
            return await _dbSet
                .Include(t => t.Portfolio)
                .Include(t => t.OwnerUser)
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.InviteCode == normalizedCode);
        }

        public async Task<bool> InviteCodeExistsAsync(string inviteCode)
        {
            var normalizedCode = inviteCode?.Trim().ToUpperInvariant() ?? string.Empty;
            return await _dbSet.AnyAsync(t => t.InviteCode == normalizedCode);
        }

        public async Task<TeamUser?> GetMembershipAsync(Guid teamId, Guid userId)
        {
            return await _context.Set<TeamUser>()
                .FirstOrDefaultAsync(tu => tu.TeamId == teamId && tu.UserId == userId);
        }

        public async Task AddMemberAsync(TeamUser teamUser)
        {
            await _context.Set<TeamUser>().AddAsync(teamUser);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(TeamUser teamUser)
        {
            _context.Set<TeamUser>().Remove(teamUser);
            await _context.SaveChangesAsync();
        }
    }
}
