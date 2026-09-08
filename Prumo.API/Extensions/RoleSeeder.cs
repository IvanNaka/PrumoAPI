using Microsoft.EntityFrameworkCore;
using Plantonize.Plantao.Infrastructure;
using Prumo.Domain.Entities;
using Prumo.Domain.Enums;

namespace Prumo.API.Extensions
{
    /// <summary>
    /// Ensures every value of <see cref="RoleName"/> exists as a Role row in the database.
    /// Idempotent: safe to run on every application startup.
    /// </summary>
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PrumoDbContext>();

            var existingNames = await context.Set<Role>()
                .Select(r => r.Name)
                .ToListAsync();

            var missingRoles = Enum.GetNames(typeof(RoleName))
                .Where(name => !existingNames.Contains(name))
                .Select(name => new Role { Name = name })
                .ToList();

            if (missingRoles.Count > 0)
            {
                await context.Set<Role>().AddRangeAsync(missingRoles);
                await context.SaveChangesAsync();
            }
        }
    }
}
