using Colabs.Common;
using Colabs.Dtos;
using Colabs.Models;

namespace Colabs.Services;

/// <summary>Cadastro público e autenticação de usuários por e-mail e senha.</summary>
public interface IAuthService
{
    /// <summary>Cadastra um novo usuário com o papel USER. Conflict se CPF ou e-mail já existirem.</summary>
    Task<ServiceResult<User>> RegisterAsync(RegisterRequest request);

    /// <summary>Devolve o token se as credenciais forem válidas; Unauthorized caso contrário.</summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
}