namespace Colabs.Common;

/// <summary>
/// Status possíveis de uma operação executada por um Service.
/// </summary>
public enum ServiceStatus
{
    Ok,
    NotFound,
    ValidationError,
    Conflict,
    Forbidden,
    Unauthorized
}

/// <summary>
/// Encapsula o resultado de uma operação de Service, permitindo que o Controller
/// traduza o desfecho da regra de negócio em uma resposta HTTP sem conhecer a regra em si.
/// Para operações sem retorno (cancelar, excluir), use <c>ServiceResult&lt;bool&gt;</c>.
/// </summary>
/// <typeparam name="T">Tipo do dado retornado em caso de sucesso.</typeparam>
public class ServiceResult<T>
{
    public ServiceStatus Status { get; }
    public T? Value { get; }
    public string? Message { get; }

    public bool IsSuccess => Status == ServiceStatus.Ok;

    private ServiceResult(ServiceStatus status, T? value, string? message)
    {
        Status = status;
        Value = value;
        Message = message;
    }

    public static ServiceResult<T> Ok(T value) =>
        new(ServiceStatus.Ok, value, null);

    /// <summary>Recurso inexistente (404).</summary>
    public static ServiceResult<T> NotFound(string message) =>
        new(ServiceStatus.NotFound, default, message);

    /// <summary>Dados ou regra de negócio violados (400).</summary>
    public static ServiceResult<T> ValidationError(string message) =>
        new(ServiceStatus.ValidationError, default, message);

    /// <summary>Conflito com o estado atual: duplicidade, transição de status inválida etc. (409).</summary>
    public static ServiceResult<T> Conflict(string message) =>
        new(ServiceStatus.Conflict, default, message);

    /// <summary>Usuário autenticado, mas sem permissão sobre o recurso (403).</summary>
    public static ServiceResult<T> Forbidden(string message) =>
        new(ServiceStatus.Forbidden, default, message);

    /// <summary>Credenciais inválidas (401).</summary>
    public static ServiceResult<T> Unauthorized(string message) =>
        new(ServiceStatus.Unauthorized, default, message);
}