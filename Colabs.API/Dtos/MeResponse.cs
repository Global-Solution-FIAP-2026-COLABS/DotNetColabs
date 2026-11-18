namespace Colabs.Dtos;

/// <summary>Dados do usuário autenticado, lidos das claims do token (sem consultar o banco).</summary>
public record class MeResponse(int? UserId, string? Name, string? Email, string? Role);