namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Valida o ID token emitido pelo Google e devolve o e-mail e o nome. Abstraído para permitir testes.
    /// Lança <c>InvalidJwtException</c> para token inválido e <c>HttpRequestException</c> em
    /// falha de comunicação com o Google.
    /// </summary>
    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfo> ValidateAsync(string idToken);
    }

    public record GoogleUserInfo(string Email, string? Name);
}
