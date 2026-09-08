using Prumo.Application.DTOs.Role;
using Prumo.Application.Interfaces;
using Prumo.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prumo.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles
                .OrderBy(r => r.Name)
                .Select(r => new RoleDto { Id = r.Id, Name = r.Name });
        }
    }
}
