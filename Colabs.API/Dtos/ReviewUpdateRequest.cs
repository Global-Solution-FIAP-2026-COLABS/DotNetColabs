using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Edição de uma avaliação. A doação avaliada não pode ser trocada.</summary>
public record class ReviewUpdateRequest(
    [Range(1, 5, ErrorMessage = "A nota deve estar entre 1 e 5.")]
    int Rating,

    [StringLength(500, ErrorMessage = "O comentário deve ter no máximo 500 caracteres.")]
    string? Description
);