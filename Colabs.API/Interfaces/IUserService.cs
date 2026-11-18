using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>
/// Regras de negócio da gestão de usuários. O cadastro público fica no <see cref="IAuthService"/>.
/// Leitura, edição e exclusão: somente o próprio usuário ou um ADMIN (caso contrário, Forbidden).
/// </summary>
public interface IUserService
{
    Task<ServiceResult<PagedResult<User>>> GetAllAsync(UserQueryParameters query);
    Task<ServiceResult<User>> GetByIdAsync(int id, Requester requester);
    Task<ServiceResult<bool>> UpdateAsync(int id, UserUpdateRequest request, Requester requester);

    /// <summary>Conflict se o usuário tiver pedidos ou doações, ou se for o último administrador.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester);

    /// <summary>Somente ADMIN. Um admin não rebaixa a si mesmo e o último admin é protegido.</summary>
    Task<ServiceResult<User>> ChangeRoleAsync(int id, UserRole role, Requester requester);
}