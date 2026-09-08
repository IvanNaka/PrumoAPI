using Prumo.Application.DTOs.Role;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllAsync();
    }
}
