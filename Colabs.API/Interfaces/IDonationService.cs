using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Regras de negócio das doações.</summary>
public interface IDonationService
{
    Task<ServiceResult<PagedResult<Donation>>> GetAllAsync(DonationQueryParameters query);
    Task<ServiceResult<Donation>> GetByIdAsync(int id);

    /// <summary>Cria a doação (status Available) em nome do solicitante.</summary>
    Task<ServiceResult<Donation>> CreateAsync(DonationRequest request, Requester requester);

    /// <summary>Somente doador ou admin, e somente com a doação Available. Substitui a lista de itens.</summary>
    Task<ServiceResult<bool>> UpdateAsync(int id, DonationRequest request, Requester requester);

    /// <summary>Doador ou admin (e, para Delivered, o dono do pedido vinculado). Conflict se a transição não for permitida.</summary>
    Task<ServiceResult<Donation>> ChangeStatusAsync(int id, DonationStatus status, Requester requester);

    /// <summary>Somente doador ou admin. Conflict se a doação estiver Matched ou Delivered.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester);
}