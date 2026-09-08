using Prumo.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prumo.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task UpdateAsync(UpdateUserDto dto);
        Task DeleteAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
    }
}
