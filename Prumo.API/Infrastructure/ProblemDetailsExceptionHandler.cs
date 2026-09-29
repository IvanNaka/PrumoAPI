using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.Common;

namespace Prumo.API.Infrastructure
{
    /// <summary>
    /// Converte <see cref="BusinessRuleException"/> em ProblemDetails (application/problem+json),
    /// com a mensagem da regra no campo <c>detail</c>. Exceções inesperadas viram 500 genérico,
    /// sem expor stack trace.
    /// </summary>
    public class ProblemDetailsExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

        public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            ProblemDetails problem;
            if (exception is BusinessRuleException rule)
            {
                problem = rule.Errors is { Count: > 0 }
                    ? new ValidationProblemDetails(rule.Errors) { Status = rule.Status, Detail = rule.Message }
                    : new ProblemDetails { Status = rule.Status, Detail = rule.Message };
                problem.Title = ProblemTitles.For(rule.Status);
            }
            else
            {
                _logger.LogError(exception, "Erro inesperado ao processar {Method} {Path}.",
                    httpContext.Request.Method, httpContext.Request.Path);
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = ProblemTitles.For(500),
                    Detail = "Ocorreu um erro inesperado. Tente novamente.",
                };
            }

            await ProblemWriter.WriteAsync(httpContext, problem, cancellationToken);
            return true;
        }
    }

    public static class ProblemTitles
    {
        public static string For(int status) => status switch
        {
            400 => "Requisição inválida",
            401 => "Não autenticado",
            403 => "Acesso negado",
            404 => "Não encontrado",
            409 => "Conflito",
            503 => "Serviço indisponível",
            _ => "Erro",
        };
    }

    public static class ProblemWriter
    {
        public static async Task WriteAsync(HttpContext httpContext, ProblemDetails problem, CancellationToken cancellationToken = default)
        {
            httpContext.Response.StatusCode = problem.Status ?? 500;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }
    }
}
