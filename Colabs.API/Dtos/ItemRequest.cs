using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Item de um pedido de ajuda ou de uma doação.</summary>
public record class ItemRequest(
    [Required(ErrorMessage = "O nome do item é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome do item deve ter no máximo 100 caracteres.")]
    string Name,

    [Range(1, 99999999, ErrorMessage = "A quantidade deve estar entre 1 e 99.999.999.")]
    int Quantity
);