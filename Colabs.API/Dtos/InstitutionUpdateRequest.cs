using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Atualização de instituição. O CNPJ não pode ser alterado depois do cadastro.</summary>
public record class InstitutionUpdateRequest(
    [Required(ErrorMessage = "O nome da instituição é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(400, MinimumLength = 5, ErrorMessage = "O endereço deve ter entre 5 e 400 caracteres.")]
    string Address,

    [Required(ErrorMessage = "A latitude é obrigatória.")]
    [Range(-90.0, 90.0, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
    decimal? Latitude,

    [Required(ErrorMessage = "A longitude é obrigatória.")]
    [Range(-180.0, 180.0, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
    decimal? Longitude
);