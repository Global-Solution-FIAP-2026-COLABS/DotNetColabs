using System.ComponentModel.DataAnnotations;

namespace Colabs.Dtos;

/// <summary>Criação e edição de pedido de ajuda. O dono do pedido é o usuário autenticado.</summary>
public record class HelpRequestRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Informe a categoria.")]
    int IdCategory,

    int? IdInstitution,

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "A descrição deve ter entre 5 e 500 caracteres.")]
    string Description,

    [Required(ErrorMessage = "Informe os itens do pedido.")]
    [MinLength(1, ErrorMessage = "Informe ao menos um item.")]
    List<ItemRequest> Items
);