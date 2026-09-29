using Microsoft.EntityFrameworkCore;
using Plantonize.Plantao.Infrastructure;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.API.Extensions
{
    /// <summary>
    /// Seed do primeiro administrador (T02): se não existir nenhum usuário com o perfil
    /// Administrador, cria um com o e-mail de <c>Admin:Email</c>. Idempotente.
    /// </summary>
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
            var email = configuration["Admin:Email"]?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();
            if (await db.UserRoles.AnyAsync(r => r.Role == RoleName.Administrador))
            {
                return;
            }

            var user = await db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User { Name = email, Email = email, IsActive = true };
                db.Users.Add(user);
            }

            user.IsActive = true;
            user.Roles.Add(new UserRole { UserId = user.Id, Role = RoleName.Administrador });
            await db.SaveChangesAsync();
            logger.LogInformation("Administrador inicial criado: {Email}", email);
        }
    }
}
