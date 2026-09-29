using Prumo.Application.Common;
using Prumo.Domain.Enums;

namespace Prumo.Application.StateMachines
{
    /// <summary>
    /// Avaliação do projeto (Figura 28, Seção 3.4.3). "CalculandoScore" é transitório e não é gravado.
    /// </summary>
    public static class EvaluationStateMachine
    {
        public const string PrimeiraNota = "PrimeiraNota";
        public const string Priorizar = "Priorizar";
        public const string CriterioNovo = "CriterioNovo";
        public const string NotasCompletas = "NotasCompletas";
        public const string Aprovar = "Aprovar";
        public const string Rejeitar = "Rejeitar";

        private static readonly Dictionary<(EvaluationStatus, string), EvaluationStatus> T = new()
        {
            {(EvaluationStatus.NaoAvaliado, PrimeiraNota),   EvaluationStatus.Avaliando},
            {(EvaluationStatus.Avaliando,   Priorizar),      EvaluationStatus.Priorizado},
            {(EvaluationStatus.Priorizado,  Priorizar),      EvaluationStatus.Priorizado},
            {(EvaluationStatus.Reavaliado,  Priorizar),      EvaluationStatus.Priorizado},
            {(EvaluationStatus.Priorizado,  CriterioNovo),   EvaluationStatus.Reavaliado},
            {(EvaluationStatus.Aprovado,    CriterioNovo),   EvaluationStatus.Reavaliado},
            {(EvaluationStatus.Reavaliado,  NotasCompletas), EvaluationStatus.Priorizado},
            {(EvaluationStatus.Priorizado,  Aprovar),        EvaluationStatus.Aprovado},
            {(EvaluationStatus.Priorizado,  Rejeitar),       EvaluationStatus.Rejeitado},
        };

        public static EvaluationStatus Aplicar(EvaluationStatus atual, string evento)
            => T.TryGetValue((atual, evento), out var novo)
               ? novo
               : throw new BusinessRuleException(409, Messages.RN22_Transicao(atual, evento));

        /// <summary>Para eventos automáticos: aplica se a transição existir; senão mantém o estado.</summary>
        public static EvaluationStatus TentarAplicar(EvaluationStatus atual, string evento)
            => T.TryGetValue((atual, evento), out var novo) ? novo : atual;
    }
}
