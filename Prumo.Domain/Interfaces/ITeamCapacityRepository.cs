using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface ITeamCapacityRepository : IRepository<TeamCapacityEntry>
    {
        new Task<TeamCapacityEntry?> GetByIdAsync(Guid id);
        Task<IEnumerable<TeamCapacityEntry>> GetByTeamIdAsync(Guid teamId);
        Task<TeamCapacityEntry?> GetByTeamUserYearMonthAsync(Guid teamId, Guid userId, int year, int month);
    }
}
