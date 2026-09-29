using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prumo.Application.Common;
using Prumo.Application.DTOs.User;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // RF03 — gestão de usuários (somente Administrador).
    public class UserService : IUserService
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public UserService(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _db.Users.Include(u => u.Roles).AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _db.Users.Include(u => u.Roles).AsNoTracking().OrderBy(u => u.Name).ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<UserOptionDto>> GetActiveOptionsAsync()
        {
            return await _db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new UserOptionDto { Id = u.Id, Nome = u.Name, Email = u.Email })
                .ToListAsync();
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
            ValidateRequired(dto.Nome, email, validateEmail: true);
            var roles = ParseRoles(dto.Perfis);

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                throw new BusinessRuleException(409, "Já existe um usuário com este e-mail.");
            }

            var user = new User
            {
                Name = dto.Nome.Trim(),
                Email = email,
                IsActive = true,
            };
            foreach (var role in roles)
            {
                user.Roles.Add(new UserRole { UserId = user.Id, Role = role });
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Id == id)
                ?? throw Messages.NotFound("Usuário não encontrado.");
            ValidateRequired(dto.Nome, user.Email, validateEmail: false);
            var roles = ParseRoles(dto.Perfis);

            user.Name = dto.Nome.Trim();
            user.UpdatedDate = DateTime.UtcNow;

            foreach (var existing in user.Roles.Where(r => !roles.Contains(r.Role)).ToList())
            {
                user.Roles.Remove(existing);
            }
            foreach (var role in roles.Where(r => user.Roles.All(ur => ur.Role != r)))
            {
                user.Roles.Add(new UserRole { UserId = user.Id, Role = role });
            }

            await _db.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task SetActiveAsync(Guid id, bool active)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id)
                ?? throw Messages.NotFound("Usuário não encontrado.");

            if (!active && _currentUser.UserId == id)
            {
                throw new BusinessRuleException(409, "Você não pode desativar o próprio usuário.");
            }

            user.IsActive = active;
            user.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private static void ValidateRequired(string? nome, string email, bool validateEmail)
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(nome))
            {
                errors["nome"] = new[] { "O nome é obrigatório." };
            }

            if (validateEmail && string.IsNullOrWhiteSpace(email))
            {
                errors["email"] = new[] { "O e-mail é obrigatório." };
            }

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(400, Messages.RN04_CamposObrigatorios, errors);
            }

            if (validateEmail && !IsValidEmail(email))
            {
                throw new BusinessRuleException(400, "Informe um e-mail válido.",
                    new Dictionary<string, string[]> { ["email"] = new[] { "Informe um e-mail válido." } });
            }
        }

        private static bool IsValidEmail(string email) =>
            email.Length <= 200 && Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private static List<RoleName> ParseRoles(IEnumerable<string>? perfis)
        {
            var roles = new List<RoleName>();
            foreach (var perfil in perfis ?? Enumerable.Empty<string>())
            {
                if (Enum.TryParse<RoleName>(perfil, ignoreCase: true, out var role) && Enum.IsDefined(role) && !roles.Contains(role))
                {
                    roles.Add(role);
                }
            }

            if (roles.Count == 0)
            {
                throw new BusinessRuleException(400, "Selecione ao menos um perfil.");
            }

            return roles;
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            Nome = user.Name,
            Email = user.Email,
            Perfis = user.Roles.Select(r => r.Role.ToString()).OrderBy(r => r).ToList(),
            Ativo = user.IsActive,
            DataCriacao = user.CreatedDate,
        };
    }
}
