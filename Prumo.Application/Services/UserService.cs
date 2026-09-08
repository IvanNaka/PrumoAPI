using Prumo.Application.Common;
using Prumo.Application.DTOs.User;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prumo.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllWithRoleAsync();
            return users.Select(MapToDto);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var existing = await _userRepository.GetByEmailAsync(dto.Email);
            if (existing != null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                RoleId = dto.RoleId
            };

            var created = await _userRepository.AddAsync(user);
            var createdWithRole = await _userRepository.GetByIdWithRoleAsync(created.Id);

            return MapToDto(createdWithRole ?? created);
        }

        public async Task UpdateAsync(UpdateUserDto dto)
        {
            var existing = await _userRepository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!string.Equals(existing.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailOwner = await _userRepository.GetByEmailAsync(dto.Email);
                if (emailOwner != null && emailOwner.Id != dto.Id)
                {
                    throw new InvalidOperationException("A user with this email already exists.");
                }
            }

            existing.Name = dto.Name;
            existing.Email = dto.Email;
            existing.RoleId = dto.RoleId;
            existing.UpdatedDate = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                existing.PasswordHash = PasswordHasher.Hash(dto.Password);
            }

            await _userRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _userRepository.DeleteAsync(id);
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                CreatedDate = user.CreatedDate
            };
        }
    }
}
