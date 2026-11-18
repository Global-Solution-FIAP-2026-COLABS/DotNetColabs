using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Atualização dos dados cadastrais. CPF, senha e papel não podem ser alterados por aqui.</summary>
public record class UserUpdateRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [RegularExpression(@"^\+?[0-9()\s-]{8,15}$", ErrorMessage = "Telefone inválido (8 a 15 caracteres: dígitos, espaço, parênteses, hífen).")]
    string Phone,

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(150, ErrorMessage = "O e-mail deve ter no máximo 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(400, MinimumLength = 5, ErrorMessage = "O endereço deve ter entre 5 e 400 caracteres.")]
    string Address,

    [Range(-90.0, 90.0, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
    decimal? Latitude = null,

    [Range(-180.0, 180.0, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
    decimal? Longitude = null
);  