using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class DonationResponse(
    int Id,
    int IdCategory,
    string CategoryName,
    int? IdUser,
    int? IdInstitution,
    int? IdHelpRequest,
    string Description,
    DonationStatus Status,
    DateTime CreationDate,
    DateTime? CompletionDate,
    List<ItemResponse> Items,
    List<LinkDto> Links
)
{
    /// <summary>Requer a Category e os Items carregados na entidade.</summary>
    public static DonationResponse FromEntity(Donation donation, List<LinkDto> links) => new(
        donation.Id,
        donation.IdCategory,
        donation.Category.Name,
        donation.IdUser,
        donation.IdInstitution,
        donation.IdHelpRequest,
        donation.Description,
        donation.Status,
        donation.CreationDate,
        donation.CompletionDate,
        donation.Items.OrderBy(i => i.Id).Select(i => new ItemResponse(i.Id, i.Name, i.Quantity)).ToList(),
        links);
}