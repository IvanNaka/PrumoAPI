using Prumo.Domain.Entities;

namespace Prumo.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithRoleAsync(Guid id);
        Task<IEnumerable<User>> GetAllWithRoleAsync();
    }
}
