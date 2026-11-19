using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de doações.</summary>
public class DonationQueryParameters : QueryParameters
{
    /// <summary>Status exato: Available, Matched, Delivered ou Canceled.</summary>
    public DonationStatus? Status { get; set; }

    /// <summary>Id da categoria.</summary>
    public int? IdCategory { get; set; }

    /// <summary>Id do usuário doador.</summary>
    public int? IdUser { get; set; }

    /// <summary>Id do pedido de ajuda atendido pela doação.</summary>
    public int? IdHelpRequest { get; set; }

    /// <summary>Descrição contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Description { get; set; }
}