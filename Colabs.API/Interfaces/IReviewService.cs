using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Regras de negócio da avaliação de entregas.</summary>
public interface IReviewService
{
    Task<ServiceResult<PagedResult<Review>>> GetAllAsync(ReviewQueryParameters query);
    Task<ServiceResult<Review>> GetByIdAsync(int id);

    /// <summary>Somente quem recebeu a doação (ou admin). Conflict se a doação não foi entregue ou já foi avaliada.</summary>
    Task<ServiceResult<Review>> CreateAsync(ReviewRequest request, Requester requester);

    Task<ServiceResult<bool>> UpdateAsync(int id, ReviewUpdateRequest request, Requester requester);
    Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester);
}