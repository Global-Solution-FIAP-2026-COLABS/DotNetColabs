using System.ComponentModel.DataAnnotations;

namespace Colabs.Common.Validation;

/// <summary>
/// Valida CNPJ em DTOs. Aceita com ou sem pontuação. Valor vazio é ignorado
/// (combine com [Required] quando o campo for obrigatório).
/// </summary>
public class CnpjAttribute : ValidationAttribute
{
    public CnpjAttribute() : base("CNPJ inválido.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || string.IsNullOrWhiteSpace(text) || CnpjValidator.IsValid(text);
}