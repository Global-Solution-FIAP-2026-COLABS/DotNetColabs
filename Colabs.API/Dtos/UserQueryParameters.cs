using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de usuários (somente admin).</summary>
public class UserQueryParameters : QueryParameters
{
    /// <summary>Nome contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Name { get; set; }

    /// <summary>E-mail contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Email { get; set; }

    /// <summary>CPF contém o texto (aceita com ou sem pontuação).</summary>
    public string? Cpf { get; set; }

    /// <summary>Papel exato (User ou Admin).</summary>
    public UserRole? Role { get; set; }
}