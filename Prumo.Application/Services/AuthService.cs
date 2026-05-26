using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Prumo.Application.Interfaces;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;
using Prumo.Domain.Interfaces;

namespace Prumo.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IRoleRepository roleRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
        }

        public async Task<string> SignInWithGoogleAsync(string idToken)
        {
            // Validate token with Google
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            // Find or create user
            var email = payload.Email ?? throw new InvalidOperationException("Google token has no email");
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Ensure a default role exists (Viewer)
                var role = await _roleRepository.GetByNameAsync(RoleName.Viewer.ToString());
                if (role == null)
                {
                    role = new Role { Name = RoleName.Viewer.ToString() };
                    await _roleRepository.AddAsync(role);
                }

                user = new User
                {
                    Email = email,
                    Name = payload.Name ?? email,
                    RoleId = role.Id
                };

                user = await _userRepository.AddAsync(user);
            }

            // Generate JWT
            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiresMinutes = int.TryParse(_configuration["Jwt:ExpiresMinutes"], out var m) ? m : 60;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            };

            // optionally add role claim if Role navigation not loaded
            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Name.ToString()));
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}