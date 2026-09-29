namespace Prumo.Application.Common
{
    /// <summary>
    /// Violação de regra de negócio (RegraNegocioException, Seção 3.3). É convertida em
    /// ProblemDetails (<c>application/problem+json</c>) com a mensagem no campo <c>detail</c>.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(int status, string detail, IDictionary<string, string[]>? errors = null)
            : base(detail)
        {
            Status = status;
            Errors = errors;
        }

        public int Status { get; }

        /// <summary>Lista de campos inválidos (RN04), devolvida em <c>errors</c>.</summary>
        public IDictionary<string, string[]>? Errors { get; }
    }
}
