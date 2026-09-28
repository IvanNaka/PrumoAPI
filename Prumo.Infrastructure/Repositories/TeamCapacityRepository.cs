using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;

namespace Plantonize.Plantao.Infrastructure.Repositories
{
    public class TeamCapacityRepository : Repository<TeamCapacityEntry>, ITeamCapacityRepository
    {
        public TeamCapacityRepository(PrumoDbContext context) : base(context)
        {
        }

        public async Task<TeamCapacityEntry?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<TeamCapacityEntry>> GetByTeamIdAsync(Guid teamId)
        {
            return await _dbSet
                .Where(c => c.TeamId == teamId)
                .Include(c => c.User)
                .ToListAsync();
        }

        public async Task<TeamCapacityEntry?> GetByTeamUserYearMonthAsync(Guid teamId, Guid userId, int year, int month)
        {
            return await _dbSet
                .Include(c => c.User)
                .FirstOrDefaultAsync(c =>
                    c.TeamId == teamId && c.UserId == userId && c.Year == year && c.Month == month);
        }
    }
}
