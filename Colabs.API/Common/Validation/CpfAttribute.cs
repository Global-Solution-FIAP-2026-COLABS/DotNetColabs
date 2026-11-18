using System.ComponentModel.DataAnnotations;

namespace Colabs.Common.Validation;

/// <summary>
/// Valida CPF em DTOs. Aceita com ou sem pontuação. Valor vazio é ignorado
/// (combine com [Required] quando o campo for obrigatório).
/// </summary>
public class CpfAttribute : ValidationAttribute
{
    public CpfAttribute() : base("CPF inválido.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || string.IsNullOrWhiteSpace(text) || CpfValidator.IsValid(text);
}