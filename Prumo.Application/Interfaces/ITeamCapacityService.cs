using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prumo.Application.DTOs.TeamCapacity;

namespace Prumo.Application.Interfaces
{
    public interface ITeamCapacityService
    {
        Task<IEnumerable<TeamCapacityEntryDto>> GetByTeamIdAsync(Guid teamId);
        Task<(TeamCapacityEntryDto Entry, bool Created)> UpsertAsync(CreateTeamCapacityDto dto);
        Task UpdateAsync(UpdateTeamCapacityDto dto);
        Task DeleteAsync(Guid id);
        Task<TeamCapacityEntryDto> GetByIdAsync(Guid id);
    }
}
