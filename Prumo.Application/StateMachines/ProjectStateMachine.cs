using Prumo.Application.Common;
using Prumo.Domain.Enums;

namespace Prumo.Application.StateMachines
{
    /// <summary>
    /// Ciclo de vida do projeto (Figura 26 + D04, Seção 3.4.1). Qualquer transição fora da tabela
    /// devolve RN22.
    /// </summary>
    public static class ProjectStateMachine
    {
        private static readonly Dictionary<(ProjectStatus, string), ProjectStatus> T = new()
        {
            {(ProjectStatus.Rascunho,    "Aprovar"),      ProjectStatus.Planejado},
            {(ProjectStatus.Planejado,   "Iniciar"),      ProjectStatus.EmAndamento},
            {(ProjectStatus.EmAndamento, "MarcarRisco"),  ProjectStatus.EmRisco},
            {(ProjectStatus.EmRisco,     "MitigarRisco"), ProjectStatus.EmAndamento},
            {(ProjectStatus.EmAndamento, "Suspender"),    ProjectStatus.Suspenso},
            {(ProjectStatus.Suspenso,    "Retomar"),      ProjectStatus.EmAndamento},
            {(ProjectStatus.EmAndamento, "Finalizar"),    ProjectStatus.Concluido},
            {(ProjectStatus.Concluido,   "Arquivar"),     ProjectStatus.Arquivado},
            // Cancelar (RF13 / D04): permitido a partir de estes estados
            {(ProjectStatus.Rascunho,    "Cancelar"),     ProjectStatus.Cancelado},
            {(ProjectStatus.Planejado,   "Cancelar"),     ProjectStatus.Cancelado},
            {(ProjectStatus.EmAndamento, "Cancelar"),     ProjectStatus.Cancelado},
            {(ProjectStatus.EmRisco,     "Cancelar"),     ProjectStatus.Cancelado},
            {(ProjectStatus.Suspenso,    "Cancelar"),     ProjectStatus.Cancelado},
        };

        public static ProjectStatus Aplicar(ProjectStatus atual, string acao)
            => T.TryGetValue((atual, acao), out var novo)
               ? novo
               : throw new BusinessRuleException(409, Messages.RN22_Transicao(atual, acao));

        /// <summary>Ações válidas a partir de um status (usado pela tela para montar os botões).</summary>
        public static IReadOnlyList<string> AcoesPermitidas(ProjectStatus atual) =>
            T.Keys.Where(k => k.Item1 == atual).Select(k => k.Item2).ToList();

        /// <summary>Cancelado e Arquivado são estados finais: nenhuma edição de dados é permitida.</summary>
        public static bool EhFinal(ProjectStatus status) =>
            status is ProjectStatus.Cancelado or ProjectStatus.Arquivado;
    }
}
