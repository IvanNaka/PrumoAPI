namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Valida o ID token emitido pelo Google e devolve o e-mail. Abstraído para permitir testes.
    /// Lança <c>InvalidJwtException</c> para token inválido e <c>HttpRequestException</c> em
    /// falha de comunicação com o Google.
    /// </summary>
    public interface IGoogleTokenValidator
    {
        Task<string> ValidateAndGetEmailAsync(string idToken);
    }
}
