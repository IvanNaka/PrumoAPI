using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Prumo.Application.Interfaces;

namespace Prumo.Application.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly IConfiguration _configuration;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> ValidateAndGetEmailAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"] ?? _configuration["Authentication:Google:ClientId"];
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new InvalidJwtException("Token sem e-mail.");
            }

            return payload.Email;
        }
    }
}
