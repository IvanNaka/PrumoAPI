using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Prumo.API.Infrastructure;
using Prumo.Application.Common;

namespace Prumo.API.Authorization
{
    /// <summary>
    /// 403 gerado por policy de perfil -> ProblemDetails com a mensagem RN27 (ou, para quem ainda não
    /// tem perfil, a orientação de entrar em uma equipe ou criar uma).
    /// </summary>
    public class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _default = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                await ProblemWriter.WriteAsync(context, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = ProblemTitles.For(403),
                    Detail = Policies.HasAnyRole(context.User) ? Messages.RN27_PerfilSemPermissao : Messages.SemEquipe,
                });
                return;
            }

            await _default.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
