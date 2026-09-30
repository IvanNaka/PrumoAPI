using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Prumo.Application.Common;
using Prumo.Application.DTOs.Auth;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    // UC1 / RF01 / D01: login exclusivo com Google -> validação no back-end -> JWT próprio.
    public class AuthService : IAuthService
    {
        private readonly IAppDbContext _db;
        private readonly IGoogleTokenValidator _googleValidator;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUser;

        public AuthService(
            IAppDbContext db,
            IGoogleTokenValidator googleValidator,
            IConfiguration configuration,
            ICurrentUserService currentUser)
        {
            _db = db;
            _googleValidator = googleValidator;
            _configuration = configuration;
            _currentUser = currentUser;
        }

        public async Task<LoginResponseDto> LoginGoogleAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new BusinessRuleException(401, Messages.RN01_TokenInvalido);
            }

            GoogleUserInfo google;
            try
            {
                google = await _googleValidator.ValidateAsync(idToken);
            }
            catch (InvalidJwtException)
            {
                throw new BusinessRuleException(401, Messages.RN01_TokenInvalido);
            }
            catch (HttpRequestException)
            {
                throw new BusinessRuleException(503, Messages.RN02_FalhaGoogle);
            }

            var email = google.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Email == email)
                ?? await RegisterAsync(email, google.Name);
            if (!user.IsActive)
            {
                throw new BusinessRuleException(403, Messages.RN03_SemPermissao);
            }

            return IssueSession(user);
        }

        public async Task<LoginResponseDto> RefreshSessionAsync(Guid userId)
        {
            var user = await _db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Id == userId);
            if (user is null || !user.IsActive)
            {
                throw new BusinessRuleException(403, Messages.RN03_SemPermissao);
            }

            return IssueSession(user);
        }

        /// <summary>
        /// Primeiro acesso de um e-mail não cadastrado: cria o usuário. Se o e-mail já foi incluído como
        /// membro de alguma equipe, ele entra como Desenvolvedor; senão fica sem perfil e só acessa a
        /// tela de entrar/criar equipe.
        /// </summary>
        private async Task<User> RegisterAsync(string email, string? name)
        {
            var memberships = await _db.TeamUsers.Where(m => m.Email == email && m.UserId == null).ToListAsync();
            var nome = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim();
            var user = new User { Name = nome.Length > 150 ? nome[..150] : nome, Email = email };
            if (memberships.Count > 0)
            {
                user.Roles.Add(new UserRole { UserId = user.Id, Role = RoleName.Desenvolvedor });
            }

            foreach (var membership in memberships)
            {
                membership.UserId = user.Id;
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private LoginResponseDto IssueSession(User user)
        {
            var expiraEm = DateTime.UtcNow.AddHours(GetExpirationHours());
            return new LoginResponseDto
            {
                Token = GenerateJwtToken(user, expiraEm),
                ExpiraEm = expiraEm,
                Usuario = MapUser(user),
            };
        }

        public async Task<AuthUserDto> GetCurrentUserAsync()
        {
            var userId = _currentUser.RequireUserId();
            var user = await _db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Id == userId);
            if (user is null || !user.IsActive)
            {
                throw new BusinessRuleException(403, Messages.RN03_SemPermissao);
            }

            return MapUser(user);
        }

        private int GetExpirationHours()
        {
            if (int.TryParse(_configuration["Jwt:ExpiraHoras"], out var hours) && hours > 0)
            {
                return hours;
            }

            return 8;
        }

        private string GenerateJwtToken(User user, DateTime expiresAt)
        {
            var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");

            // claims: sub=Id, email, name e um ClaimTypes.Role para cada perfil.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.Name),
            };
            claims.AddRange(user.Roles.Select(r => new Claim("role", r.Role.ToString())));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var token = new JwtSecurityToken(
                JwtSettings.Issuer(_configuration),
                JwtSettings.Audience(_configuration),
                claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static AuthUserDto MapUser(User user) => new()
        {
            Id = user.Id,
            Nome = user.Name,
            Email = user.Email,
            Perfis = user.Roles.Select(r => r.Role.ToString()).OrderBy(r => r).ToList(),
        };
    }

    public static class JwtSettings
    {
        public static string Issuer(IConfiguration configuration) => configuration["Jwt:Issuer"] ?? "prumo-api";
        public static string Audience(IConfiguration configuration) => configuration["Jwt:Audience"] ?? "prumo-web";
    }
}
