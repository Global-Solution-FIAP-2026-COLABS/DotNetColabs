using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Colabs.Middleware;

/// <summary>
/// Handler global de exceções não tratadas (última linha de defesa).
/// Responsável por:
///   1. Logar o erro com o CorrelationId da requisição (Error para falhas inesperadas;
///      Warning para violação de unicidade, que é um conflito esperado).
///   2. Devolver ao cliente uma resposta padronizada (RFC 7807 - ProblemDetails),
///      sem vazar detalhes internos (stack trace, mensagens de exceção do EF/Oracle, etc.).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>ORA-00001: unique constraint violated.</summary>
    private const int OracleUniqueViolation = 1;

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"] as string ?? "N/A";

        // Duas requisições simultâneas com o mesmo CPF/e-mail/CNPJ passam pela checagem prévia do Service
        // e a segunda cai na unique constraint do banco: isso é conflito (409), não erro do servidor.
        var isUniqueViolation = exception is DbUpdateException { InnerException: OracleException { Number: OracleUniqueViolation } };

        ProblemDetails problemDetails;

        if (isUniqueViolation)
        {
            _logger.LogWarning(
                exception,
                "Violação de unicidade ao processar {Method} {Path}. CorrelationId: {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Já existe um registro com os mesmos dados únicos.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Instance = httpContext.Request.Path
            };
        }
        else
        {
            _logger.LogError(
                exception,
                "Erro não tratado ao processar {Method} {Path}. CorrelationId: {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocorreu um erro inesperado ao processar a requisição.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = httpContext.Request.Path
            };
        }

        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}