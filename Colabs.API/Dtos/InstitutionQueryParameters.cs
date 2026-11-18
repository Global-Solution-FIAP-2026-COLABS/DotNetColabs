using Colabs.Common;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de instituições.</summary>
public class InstitutionQueryParameters : QueryParameters
{
    /// <summary>Nome contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Name { get; set; }

    /// <summary>CNPJ contém o texto (aceita com ou sem pontuação).</summary>
    public string? Cnpj { get; set; }

    /// <summary>Endereço contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Address { get; set; }
}