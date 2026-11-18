namespace Colabs.Common;

public static class StringExtensions
{
    /// <summary>
    /// Normaliza o e-mail (sem espaços nas pontas e em minúsculas) antes de gravar ou comparar,
    /// para que "Ana@Email.com" e "ana@email.com" sejam o mesmo e-mail.
    /// </summary>
    public static string NormalizeEmail(this string email) => email.Trim().ToLowerInvariant();
}