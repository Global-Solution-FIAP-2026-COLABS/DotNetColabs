using Colabs.Common;
using Colabs.Models;

namespace Colabs.Dtos;

public record class ReviewResponse(
    int Id,
    int IdDonation,
    int Rating,
    string? Description,
    DateTime ReviewDate,
    List<LinkDto> Links
)
{
    public static ReviewResponse FromEntity(Review review, List<LinkDto> links) => new(
        review.Id, review.IdDonation, review.Rating, review.Description, review.ReviewDate, links);
}