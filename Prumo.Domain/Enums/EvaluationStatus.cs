namespace Prumo.Domain.Enums
{
    // Figura 28. "CalculandoScore" é transitório (acontece durante o cálculo) e não é gravado.
    public enum EvaluationStatus
    {
        NaoAvaliado,
        Avaliando,
        Priorizado,
        Reavaliado,
        Aprovado,
        Rejeitado
    }
}
