using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Regras de negócio dos pedidos de ajuda.</summary>
public interface IHelpRequestService
{
    Task<ServiceResult<PagedResult<HelpRequest>>> GetAllAsync(HelpRequestQueryParameters query);
    Task<ServiceResult<HelpRequest>> GetByIdAsync(int id);

    /// <summary>Cria o pedido (status Open) em nome do solicitante.</summary>
    Task<ServiceResult<HelpRequest>> CreateAsync(HelpRequestRequest request, Requester requester);

    /// <summary>Somente dono ou admin, e somente com o pedido Open. Substitui a lista de itens.</summary>
    Task<ServiceResult<bool>> UpdateAsync(int id, HelpRequestRequest request, Requester requester);

    /// <summary>Somente dono ou admin. Conflict se a transição de status não for permitida.</summary>
    Task<ServiceResult<HelpRequest>> ChangeStatusAsync(int id, HelpRequestStatus status, Requester requester);

    /// <summary>Somente dono ou admin. Conflict se já houver doação vinculada.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester);
}