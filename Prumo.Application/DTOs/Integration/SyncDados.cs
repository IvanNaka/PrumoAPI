using Prumo.Domain.Enums;

namespace Prumo.Application.DTOs.Integration
{
    /// <summary>Issue lida da ferramenta externa, já mapeada para o modelo do Prumo.</summary>
    public record ExternalIssueData(
        string ExternalId,
        string Title,
        ExternalIssueType Type,
        string Status,
        bool Done,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        DateOnly? DueDate,
        DateTime UpdatedAt,
        decimal? EstimateHours,
        string? AssigneeEmail);

    /// <summary>Worklog lido da ferramenta externa.</summary>
    public record ExternalWorklogData(string ExternalId, string? AuthorEmail, decimal Hours, DateOnly Date);

    /// <summary>A ferramenta recusou as credenciais (HTTP 401) — RN26.</summary>
    public class IntegrationAuthException : Exception
    {
        public IntegrationAuthException(string message) : base(message) { }
    }

    /// <summary>A ferramenta está indisponível (HTTP 5xx, timeout ou falha de rede) — RN25.</summary>
    public class IntegrationUnavailableException : Exception
    {
        public IntegrationUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>Outra resposta de erro (ex.: 400/403/404 para uma chave de projeto inexistente).</summary>
    public class IntegrationRequestException : Exception
    {
        public int StatusCode { get; }

        public IntegrationRequestException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
