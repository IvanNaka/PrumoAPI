namespace Prumo.Domain.Enums
{
    /// <summary>
    /// Tipo de issue importada da ferramenta externa (TipoIssue). Usado nos indicadores de
    /// qualidade (Bug x entregas) e lead time.
    /// </summary>
    public enum ExternalIssueType
    {
        Story,
        Task,
        Feature,
        Bug
    }
}
