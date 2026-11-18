using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Colabs.Common;

public static class UserClaimsExtensions
{
    /// <summary>Id do usuário autenticado (claim "sub" do token JWT), ou null se ausente/inválido.</summary>
    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    /// <summary>true se o usuário autenticado tem o papel ADMIN.</summary>
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(AuthRoles.Admin);

    /// <summary>
    /// Monta o <see cref="Requester"/> a partir do token. Os tokens são emitidos por esta API e sempre trazem "sub";
    /// se por algum motivo faltar, o Id 0 não corresponde a nenhum usuário e as regras de dono resultam em acesso negado.
    /// </summary>
    public static Requester GetRequester(this ClaimsPrincipal user) =>
        new(user.GetUserId() ?? 0, user.IsAdmin());
}