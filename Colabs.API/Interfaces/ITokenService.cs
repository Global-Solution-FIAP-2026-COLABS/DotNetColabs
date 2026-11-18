using Colabs.Models;

namespace Colabs.Services;

/// <summary>Resultado da geração do token: o JWT e o instante (UTC) em que expira.</summary>
public record TokenResult(string Token, DateTime ExpiresAt);

/// <summary>Geração de tokens JWT para usuários autenticados.</summary>
public interface ITokenService
{
    TokenResult GenerateToken(User user);
}