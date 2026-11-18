using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class CategoryResponse(
    int Id,
    string Name,
    List<LinkDto> Links
)
{
    public static CategoryResponse FromEntity(Category category, List<LinkDto> links) => new(
        category.Id, category.Name, links);
}