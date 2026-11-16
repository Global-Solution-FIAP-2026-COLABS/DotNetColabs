using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Colabs.Data.Converters;

/// <summary>
/// Converte um enum C# (PascalCase) em texto UPPER_SNAKE_CASE no banco e vice-versa.
/// Exemplos: InProgress &lt;-&gt; "IN_PROGRESS", Open &lt;-&gt; "OPEN", Admin &lt;-&gt; "ADMIN".
/// Os valores gerados casam com os CHECK constraints definidos no schema.
/// </summary>
public class UpperSnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public UpperSnakeCaseEnumConverter()
        : base(value => ToDatabase(value), text => FromDatabase(text))
    {
    }

    private static string ToDatabase(TEnum value) =>
        Regex.Replace(value.ToString(), "(?<!^)([A-Z])", "_$1").ToUpperInvariant();

    private static TEnum FromDatabase(string text) =>
        Enum.Parse<TEnum>(text.Replace("_", string.Empty), ignoreCase: true);
}