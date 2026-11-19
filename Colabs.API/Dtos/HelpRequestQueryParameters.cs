using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Paginação, ordenação e filtros da listagem de pedidos de ajuda.</summary>
public class HelpRequestQueryParameters : QueryParameters
{
    /// <summary>Status exato: Open, InProgress, Completed ou Canceled.</summary>
    public HelpRequestStatus? Status { get; set; }

    /// <summary>Id da categoria.</summary>
    public int? IdCategory { get; set; }

    /// <summary>Id do usuário solicitante (use o seu para listar só os seus pedidos).</summary>
    public int? IdUser { get; set; }

    /// <summary>Descrição contém o texto (sem diferenciar maiúsculas).</summary>
    public string? Description { get; set; }
}