using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class HelpRequestResponse(
    int Id,
    int IdCategory,
    string CategoryName,
    int IdUser,
    int? IdInstitution,
    string Description,
    HelpRequestStatus Status,
    DateTime CreationDate,
    DateTime? CompletionDate,
    List<ItemResponse> Items,
    List<LinkDto> Links
)
{
    /// <summary>Requer a Category e os Items carregados na entidade.</summary>
    public static HelpRequestResponse FromEntity(HelpRequest request, List<LinkDto> links) => new(
        request.Id,
        request.IdCategory,
        request.Category.Name,
        request.IdUser,
        request.IdInstitution,
        request.Description,
        request.Status,
        request.CreationDate,
        request.CompletionDate,
        request.Items.OrderBy(i => i.Id).Select(i => new ItemResponse(i.Id, i.Name, i.Quantity)).ToList(),
        links);
}