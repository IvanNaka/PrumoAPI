using Prumo.Application.DTOs.Role;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // Os perfis são o enum RoleName (Seção 3.1); não há mais tabela de perfis.
    public class RoleService : IRoleService
    {
        public Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            IEnumerable<RoleDto> roles = Enum.GetNames<RoleName>().Select(name => new RoleDto { Name = name });
            return Task.FromResult(roles);
        }
    }
}
