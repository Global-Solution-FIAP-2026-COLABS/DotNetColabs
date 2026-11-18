using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Regras de negócio do cadastro de instituições (pontos de apoio).</summary>
public interface IInstitutionService
{
    Task<ServiceResult<PagedResult<Institution>>> GetAllAsync(InstitutionQueryParameters query);
    Task<ServiceResult<Institution>> GetByIdAsync(int id);

    /// <summary>Conflict se já existir instituição com o mesmo CNPJ.</summary>
    Task<ServiceResult<Institution>> CreateAsync(InstitutionRequest request);

    Task<ServiceResult<bool>> UpdateAsync(int id, InstitutionUpdateRequest request);

    /// <summary>Conflict se houver pedidos de ajuda ou doações vinculados à instituição.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int id);
}