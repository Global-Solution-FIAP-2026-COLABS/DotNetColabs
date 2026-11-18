namespace Colabs.Common.Validation;

/// <summary>Normalização e validação de CPF (11 dígitos com dígitos verificadores).</summary>
public static class CpfValidator
{
    /// <summary>Remove tudo que não for dígito ("529.982.247-25" vira "52998224725").</summary>
    public static string Normalize(string? value) =>
        new((value ?? string.Empty).Where(c => c is >= '0' and <= '9').ToArray());

    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);

        // 11 dígitos e não pode ser uma sequência repetida (111.111.111-11 passaria no cálculo).
        if (digits.Length != 11 || digits.Distinct().Count() == 1)
            return false;

        return digits[9] - '0' == CheckDigit(digits, 9)
            && digits[10] - '0' == CheckDigit(digits, 10);
    }

    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * (length + 1 - i);

        return sum * 10 % 11 % 10;
    }
}