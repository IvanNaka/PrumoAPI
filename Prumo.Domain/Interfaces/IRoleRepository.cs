using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Domain.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string name);
    }
}