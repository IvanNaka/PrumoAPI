using System.Security.Claims;
using Prumo.Application.Common;
using Prumo.Application.Interfaces;
using Prumo.Domain.Enums;

namespace Prumo.API.Infrastructure
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUserService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public IReadOnlyCollection<RoleName> Roles =>
            Principal?.FindAll(ClaimTypes.Role)
                .Select(c => Enum.TryParse<RoleName>(c.Value, out var role) ? (RoleName?)role : null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .Distinct()
                .ToList()
            ?? new List<RoleName>();

        public bool IsInRole(RoleName role) => Roles.Contains(role);

        public Guid RequireUserId() =>
            UserId ?? throw new BusinessRuleException(401, Messages.RN01_TokenInvalido);
    }
}
