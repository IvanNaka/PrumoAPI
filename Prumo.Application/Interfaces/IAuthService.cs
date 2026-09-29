using Prumo.Application.DTOs.Auth;

namespace Prumo.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>UC1: valida o ID token do Google e devolve o JWT do Prumo.</summary>
        Task<LoginResponseDto> LoginGoogleAsync(string idToken);

        /// <summary>GET /auth/me.</summary>
        Task<AuthUserDto> GetCurrentUserAsync();
    }
}
