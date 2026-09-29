using Microsoft.EntityFrameworkCore;
using Prumo.Domain.Entities;

namespace Prumo.Application.Interfaces
{
    /// <summary>
    /// Abstração do DbContext usada pelos serviços da camada de aplicação.
    /// A implementação é o <c>PrumoDbContext</c> (Infrastructure).
    /// </summary>
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Portfolio> Portfolios { get; }
        DbSet<PortfolioMember> PortfolioMembers { get; }
        DbSet<PortfolioObjective> PortfolioObjectives { get; }
        DbSet<Project> Projects { get; }
        DbSet<PriorityCriteria> PriorityCriterias { get; }
        DbSet<Objective> Objectives { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
