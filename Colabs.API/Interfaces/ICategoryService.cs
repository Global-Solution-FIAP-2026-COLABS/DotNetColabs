using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Regras de negócio do cadastro de categorias.</summary>
public interface ICategoryService
{
    Task<ServiceResult<PagedResult<Category>>> GetAllAsync(CategoryQueryParameters query);
    Task<ServiceResult<Category>> GetByIdAsync(int id);

    /// <summary>Conflict se já existir categoria com o mesmo nome (sem diferenciar maiúsculas).</summary>
    Task<ServiceResult<Category>> CreateAsync(CategoryRequest request);

    Task<ServiceResult<bool>> UpdateAsync(int id, CategoryRequest request);

    /// <summary>Conflict se a categoria estiver em uso por pedidos de ajuda ou doações.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int id);
}