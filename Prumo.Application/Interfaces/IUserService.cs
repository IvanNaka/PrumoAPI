using Prumo.Application.DTOs.User;

namespace Prumo.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<IEnumerable<UserOptionDto>> GetActiveOptionsAsync();
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
        Task SetActiveAsync(Guid id, bool active);
    }
}
