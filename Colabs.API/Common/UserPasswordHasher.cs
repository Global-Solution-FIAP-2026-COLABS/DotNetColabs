using Colabs.Models;
using Microsoft.AspNetCore.Identity;

namespace Colabs.Common;

/// <summary>
/// Hash e verificação de senha dos usuários, usando o <see cref="PasswordHasher{TUser}"/> do ASP.NET Core Identity
/// (PBKDF2 com sal aleatório e formato versionado). O hash cabe folgado nos 300 caracteres da coluna PASSWORD.
/// </summary>
public static class UserPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    // Hash de uma senha qualquer, usado só para gastar o mesmo tempo quando o e-mail não existe.
    private static readonly string DummyHash = Hasher.HashPassword(new User(), "senha-descartavel-para-igualar-tempo");

    public static string Hash(User user, string password) => Hasher.HashPassword(user, password);

    public static bool Verify(User user, string password)
    {
        if (string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(password))
            return false;

        try
        {
            var result = Hasher.VerifyHashedPassword(user, user.Password, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Valor que não é um hash válido (ex.: senha em texto puro inserida direto no banco): nunca autentica.
            return false;
        }
    }

    /// <summary>
    /// Executa uma verificação de senha sem usuário real e SEMPRE devolve false. Evita que o tempo de resposta
    /// do login revele se o e-mail existe.
    /// </summary>
    public static bool VerifyDummy(string password)
    {
        Hasher.VerifyHashedPassword(new User(), DummyHash, password ?? string.Empty);
        return false;
    }
}