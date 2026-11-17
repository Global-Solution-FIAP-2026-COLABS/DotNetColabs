using Microsoft.AspNetCore.Mvc;

namespace Colabs.Common;

public static class ServiceResultExtensions
{
    /// <summary>
    /// Converte um <see cref="ServiceResult{T}"/> que NÃO é sucesso em uma resposta ProblemDetails (RFC 7807)
    /// com o status HTTP correspondente. Uso típico no Controller:
    /// <c>return result.IsSuccess ? Ok(...) : this.ToErrorResult(result);</c>
    /// </summary>
    public static IActionResult ToErrorResult<T>(this ControllerBase controller, ServiceResult<T> result) =>
        result.Status switch
        {
            ServiceStatus.NotFound => controller.Problem(
                detail: result.Message, statusCode: StatusCodes.Status404NotFound, title: "Recurso não encontrado."),

            ServiceStatus.ValidationError => controller.Problem(
                detail: result.Message, statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida."),

            ServiceStatus.Conflict => controller.Problem(
                detail: result.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflito com o estado atual do recurso."),

            ServiceStatus.Forbidden => controller.Problem(
                detail: result.Message, statusCode: StatusCodes.Status403Forbidden, title: "Acesso negado."),

            ServiceStatus.Unauthorized => controller.Problem(
                detail: result.Message, statusCode: StatusCodes.Status401Unauthorized, title: "Não autenticado."),

            _ => throw new InvalidOperationException("Um resultado de sucesso não pode ser convertido em erro.")
        };
}