using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class HistoryResponse(
    int DonationId,
    string Role,
    DonationStatus Status,
    int IdCategory,
    string CategoryName,
    string Description,
    int? IdHelpRequest,
    DateTime CreationDate,
    DateTime? CompletionDate,
    int? Rating,
    List<ItemResponse> Items,
    List<LinkDto> Links
)
{
    /// <summary>Requer Category, Items e Reviews carregados. O papel é Donor se o usuário for o doador; senão, Recipient.</summary>
    public static HistoryResponse FromEntity(Donation donation, int userId, List<LinkDto> links) => new(
        donation.Id,
        donation.IdUser == userId ? nameof(HistoryRole.Donor) : nameof(HistoryRole.Recipient),
        donation.Status,
        donation.IdCategory,
        donation.Category.Name,
        donation.Description,
        donation.IdHelpRequest,
        donation.CreationDate,
        donation.CompletionDate,
        donation.Reviews.OrderBy(r => r.Id).Select(r => (int?)r.Rating).FirstOrDefault(),
        donation.Items.OrderBy(i => i.Id).Select(i => new ItemResponse(i.Id, i.Name, i.Quantity)).ToList(),
        links);
}