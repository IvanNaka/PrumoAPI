using Microsoft.EntityFrameworkCore;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.Application.Services
{
    /// <summary>RN26: notificação (tipo Risco) para todos os Administradores ativos.</summary>
    public class NotificationAdminNotifier : IAdminNotifier
    {
        public const string EntidadeIntegracao = "Integracao";

        private readonly IAppDbContext _db;
        private readonly INotificationService _notifications;

        public NotificationAdminNotifier(IAppDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        public async Task NotifyAdminsAsync(string message, CancellationToken cancellationToken = default)
        {
            var admins = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.Roles.Any(r => r.Role == RoleName.Administrador))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var admin in admins)
            {
                await _notifications.CreateAsync(admin, AlertType.Risco, message, EntidadeIntegracao, null, cancellationToken);
            }
        }
    }
}
