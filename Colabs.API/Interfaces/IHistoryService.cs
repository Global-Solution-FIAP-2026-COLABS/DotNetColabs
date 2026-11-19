using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Histórico de doações de um usuário.</summary>
public interface IHistoryService
{
    /// <summary>Doações em que o usuário foi doador ou recebedor. Forbidden se um usuário comum pedir o histórico de outro.</summary>
    Task<ServiceResult<PagedResult<Donation>>> GetDonationsAsync(HistoryQueryParameters query, Requester requester);
}