namespace Colabs.Common.Validation;

/// <summary>Normalização e validação de CNPJ (14 dígitos com dígitos verificadores).</summary>
public static class CnpjValidator
{
    /// <summary>Remove tudo que não for dígito ("11.222.333/0001-81" vira "11222333000181").</summary>
    public static string Normalize(string? value) =>
        new((value ?? string.Empty).Where(c => c is >= '0' and <= '9').ToArray());

    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);

        // 14 dígitos e não pode ser uma sequência repetida.
        if (digits.Length != 14 || digits.Distinct().Count() == 1)
            return false;

        return digits[12] - '0' == CheckDigit(digits, 12)
            && digits[13] - '0' == CheckDigit(digits, 13);
    }

    // Pesos de 2 a 9, da direita para a esquerda, recomeçando em 2.
    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        var weight = 2;

        for (var i = length - 1; i >= 0; i--)
        {
            sum += (digits[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}