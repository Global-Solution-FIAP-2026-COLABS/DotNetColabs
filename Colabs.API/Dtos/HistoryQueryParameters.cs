using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Papel do usuário em uma doação do histórico.</summary>
public enum HistoryRole
{
    /// <summary>O usuário foi o doador.</summary>
    Donor,

    /// <summary>O usuário foi quem recebeu (dono do pedido vinculado).</summary>
    Recipient
}

/// <summary>Paginação, ordenação e filtros do histórico de doações.</summary>
public class HistoryQueryParameters : QueryParameters
{
    /// <summary>Somente como Donor ou como Recipient. Sem valor: as duas.</summary>
    public HistoryRole? Role { get; set; }

    /// <summary>Status da doação: Available, Matched, Delivered ou Canceled.</summary>
    public DonationStatus? Status { get; set; }

    /// <summary>Doações criadas a partir desta data (inclusive).</summary>
    public DateTime? From { get; set; }

    /// <summary>Doações criadas até esta data (inclusive, dia inteiro).</summary>
    public DateTime? To { get; set; }

    /// <summary>Somente administradores: histórico de outro usuário. Padrão: o usuário autenticado.</summary>
    public int? IdUser { get; set; }
}