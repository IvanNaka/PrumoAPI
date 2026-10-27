using Prumo.Application.DTOs.Auth;

namespace Prumo.Application.Interfaces
{
    public interface IAuthService
    {
        /// <summary>UC1: valida o ID token do Google e devolve o JWT do Prumo.</summary>
        Task<LoginResponseDto> LoginGoogleAsync(string idToken);

        /// <summary>Novo JWT com os perfis atuais do usuário (ex.: depois de entrar em uma equipe).</summary>
        Task<LoginResponseDto> RefreshSessionAsync(Guid userId);

        /// <summary>
        /// Login de teste (Cap. 4, seção 4.9 b): emite o JWT de um usuário já cadastrado e ativo, sem
        /// passar pelo Google. Só é exposto pela API no ambiente de testes (ver <c>AuthTeste</c>).
        /// </summary>
        Task<LoginResponseDto> LoginTesteAsync(string email);

        /// <summary>GET /auth/me.</summary>
        Task<AuthUserDto> GetCurrentUserAsync();
    }
}
