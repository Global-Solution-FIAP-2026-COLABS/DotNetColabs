using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Avaliação de uma doação entregue (nota de 1 a 5 e comentário opcional).</summary>
public record class ReviewRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Informe a doação.")]
    int IdDonation,

    [Range(1, 5, ErrorMessage = "A nota deve estar entre 1 e 5.")]
    int Rating,

    [StringLength(500, ErrorMessage = "O comentário deve ter no máximo 500 caracteres.")]
    string? Description
);