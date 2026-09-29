using Prumo.Domain.Enums;

namespace Prumo.Application.Interfaces
{
    /// <summary>Usuário autenticado da requisição atual (lido do JWT).</summary>
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        IReadOnlyCollection<RoleName> Roles { get; }
        bool IsInRole(RoleName role);

        /// <summary>Id do usuário autenticado; lança 401 se não houver.</summary>
        Guid RequireUserId();
    }
}
