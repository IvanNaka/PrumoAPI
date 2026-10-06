using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Prumo.API.Authorization
{
    /// <summary>Policies da matriz de perfis e permissões (Seção 3.6 / RF02).</summary>
    public static class Policies
    {
        public const string GerirUsuarios = "GerirUsuarios";
        public const string EditarPortfolio = "EditarPortfolio";
        public const string GovernarPortfolio = "GovernarPortfolio";
        public const string EditarCriterios = "EditarCriterios";
        public const string EditarProjetos = "EditarProjetos";
        public const string Priorizar = "Priorizar";
        public const string EditarOkrs = "EditarOkrs";
        public const string VerFinanceiro = "VerFinanceiro";
        public const string EditarFinanceiro = "EditarFinanceiro";
        public const string EditarDependencias = "EditarDependencias";
        public const string EditarEquipes = "EditarEquipes";
        public const string Integracoes = "Integracoes";
        public const string Relatorios = "Relatorios";

        /// <summary>Só exige login: usado pelas rotas liberadas para quem ainda não tem perfil.</summary>
        public const string Autenticado = "Autenticado";

        public static void Register(AuthorizationOptions o)
        {
            // [Authorize] sem policy exige ao menos um perfil: o usuário sem perfil (primeiro acesso,
            // ainda sem equipe) só acessa as rotas marcadas com a policy Autenticado.
            o.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(c => HasAnyRole(c.User))
                .Build();
            o.AddPolicy(Autenticado,        p => p.RequireAuthenticatedUser());

            o.AddPolicy(GerirUsuarios,      p => p.RequireRole("Administrador"));
            o.AddPolicy(EditarPortfolio,    p => p.RequireRole("Administrador", "ProductOwner", "GerenteProjeto"));
            o.AddPolicy(GovernarPortfolio,  p => p.RequireRole("Administrador", "GerenteProjeto", "Diretoria"));
            o.AddPolicy(EditarCriterios,    p => p.RequireRole("Administrador", "ProductOwner"));
            o.AddPolicy(EditarProjetos,     p => p.RequireRole("Administrador", "GerenteProjeto"));
            o.AddPolicy(Priorizar,          p => p.RequireRole("Administrador", "ProductOwner"));
            o.AddPolicy(EditarOkrs,         p => p.RequireRole("Administrador", "ProductOwner"));
            o.AddPolicy(VerFinanceiro,      p => p.RequireRole("Administrador", "GerenteProjeto", "ProductOwner", "Diretoria"));
            o.AddPolicy(EditarFinanceiro,   p => p.RequireRole("Administrador", "GerenteProjeto"));
            o.AddPolicy(EditarDependencias, p => p.RequireRole("Administrador", "TechLead", "GerenteProjeto"));
            o.AddPolicy(EditarEquipes,      p => p.RequireRole("Administrador", "TechLead"));
            o.AddPolicy(Integracoes,        p => p.RequireRole("Administrador", "Desenvolvedor"));
            o.AddPolicy(Relatorios,         p => p.RequireRole("Administrador", "GerenteProjeto", "Diretoria", "ProductOwner"));
        }

        public static bool HasAnyRole(ClaimsPrincipal user) =>
            user.Identities.Any(i => i.IsAuthenticated && i.Claims.Any(c => c.Type == i.RoleClaimType));
    }
}
