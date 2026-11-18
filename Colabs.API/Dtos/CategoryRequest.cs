using System.ComponentModel.DataAnnotations;
using Colabs.Models;

namespace Colabs.Dtos;

public record class CategoryRequest(
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 50 caracteres.")]
    string Name
)
{
    public Category ToEntity()
    {
        return new Category { Name = Name.Trim() };
    }
}