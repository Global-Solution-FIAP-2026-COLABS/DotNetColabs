namespace Colabs.Common;

/// <summary>
/// Quem está executando a operação: Id do usuário (claim "sub" do token) e se é ADMIN.
/// Os Services recebem este objeto para aplicar regras de "dono ou admin", sem depender do ASP.NET.
/// </summary>
public record Requester(int UserId, bool IsAdmin);