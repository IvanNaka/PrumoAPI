using System;

namespace Prumo.Application.Exceptions
{
    /// <summary>
    /// Raised when the credentials/token are rejected by the external tool (UC15 exception flow
    /// on configuration, and UC16 exception flow: "Token expirado" -> the sync is halted and
    /// re-authentication is requested).
    /// </summary>
    public class IntegrationAuthenticationException : Exception
    {
        public IntegrationAuthenticationException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
