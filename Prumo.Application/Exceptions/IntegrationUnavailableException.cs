using System;

namespace Prumo.Application.Exceptions
{
    /// <summary>
    /// Raised when the external tool's API cannot be reached during a sync (UC16 exception flow:
    /// "API indisponível" -> the system logs the error and schedules a retry).
    /// </summary>
    public class IntegrationUnavailableException : Exception
    {
        public IntegrationUnavailableException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
