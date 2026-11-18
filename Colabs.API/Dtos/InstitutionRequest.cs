using System.ComponentModel.DataAnnotations;
using Colabs.Common.Validation;
using Colabs.Models;

namespace Colabs.Dtos;

/// <summary>Cadastro de instituição. Latitude e longitude são obrigatórias: a instituição é um ponto de apoio no mapa.</summary>
public record class InstitutionRequest(
    [Required(ErrorMessage = "O nome da instituição é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    string Name,

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [Cnpj]
    string Cnpj,

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(400, MinimumLength = 5, ErrorMessage = "O endereço deve ter entre 5 e 400 caracteres.")]
    string Address,

    [Required(ErrorMessage = "A latitude é obrigatória.")]
    [Range(-90.0, 90.0, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
    decimal? Latitude,

    [Required(ErrorMessage = "A longitude é obrigatória.")]
    [Range(-180.0, 180.0, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
    decimal? Longitude
)
{
    /// <summary>Converte em entidade com o CNPJ normalizado (só dígitos). Latitude e longitude já foram validadas.</summary>
    public Institution ToEntity()
    {
        return new Institution
        {
            Name = Name.Trim(),
            Cnpj = Common.Validation.CnpjValidator.Normalize(Cnpj),
            Address = Address.Trim(),
            Latitude = Latitude!.Value,
            Longitude = Longitude!.Value
        };
    }
}