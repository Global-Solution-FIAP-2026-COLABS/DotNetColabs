using Colabs.Common;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de avaliações.</summary>
public class ReviewQueryParameters : QueryParameters
{
    /// <summary>Id da doação avaliada.</summary>
    public int? IdDonation { get; set; }

    /// <summary>Nota exata (1 a 5).</summary>
    public int? Rating { get; set; }
}