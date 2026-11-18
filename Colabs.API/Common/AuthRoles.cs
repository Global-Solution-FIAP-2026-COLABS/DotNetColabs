using Colabs.Models;

namespace Colabs.Common;

/// <summary>Nomes dos papéis (roles) usados no token JWT e em [Authorize(Roles = ...)].</summary>
public static class AuthRoles
{
    public const string User = "USER";
    public const string Admin = "ADMIN";

    /// <summary>Converte o enum do domínio no nome do papel usado no token.</summary>
    public static string From(UserRole role) => role.ToString().ToUpperInvariant();
}

/// <summary>Nomes das policies de autorização registradas na aplicação.</summary>
public static class AuthPolicies
{
    /// <summary>Exige o papel ADMIN.</summary>
    public const string AdminOnly = "AdminOnly";
}