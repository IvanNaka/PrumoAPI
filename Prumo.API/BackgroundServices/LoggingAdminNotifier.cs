using Prumo.Application.Interfaces;

namespace Prumo.API.BackgroundServices
{
    /// <summary>
    /// Aviso aos Administradores (RN26). Registra no log; substituído pelas notificações do sistema em T21.
    /// </summary>
    public class LoggingAdminNotifier : IAdminNotifier
    {
        private readonly ILogger<LoggingAdminNotifier> _logger;

        public LoggingAdminNotifier(ILogger<LoggingAdminNotifier> logger)
        {
            _logger = logger;
        }

        public Task NotifyAdminsAsync(string message, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Aviso aos administradores: {Message}", message);
            return Task.CompletedTask;
        }
    }
}
