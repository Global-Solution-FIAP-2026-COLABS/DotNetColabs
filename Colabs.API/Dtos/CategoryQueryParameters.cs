using Colabs.Common;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de categorias.</summary>
public class CategoryQueryParameters : QueryParameters
{
    /// <summary>Nome contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Name { get; set; }
}