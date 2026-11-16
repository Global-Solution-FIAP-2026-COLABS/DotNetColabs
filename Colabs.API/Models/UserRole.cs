namespace Colabs.Models;

/// <summary>
/// Papel de acesso do usuário. Persistido como texto (USER, ADMIN) na coluna DB_USER.ROLE.
/// </summary>
public enum UserRole
{
    /// <summary>Usuário comum: gerencia os próprios pedidos, doações e avaliações.</summary>
    User,

    /// <summary>Administrador: além do acesso comum, gerencia categorias e instituições.</summary>
    Admin
}