using Prumo.Domain.Enums;

namespace Prumo.Domain.Entities
{
    // UsuarioPerfil (UsuarioId, Role) — chave composta.
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public RoleName Role { get; set; }
    }
}
