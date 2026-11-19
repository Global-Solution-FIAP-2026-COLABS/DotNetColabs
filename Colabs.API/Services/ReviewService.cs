using System.Diagnostics;
using System.Diagnostics.Metrics;
using Colabs.Common;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class ReviewService : IReviewService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private readonly AppDbContext _context;
    private readonly ILogger<ReviewService> _logger;
    private readonly Counter<int> _reviewsCreatedCounter;

    public ReviewService(AppDbContext context, ILogger<ReviewService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _reviewsCreatedCounter = meter.CreateCounter<int>("reviews_created_total", description: "Total de avaliações criadas");
    }

    private IQueryable<Review> WithDonation() =>
        _context.Reviews.Include(r => r.Donation).ThenInclude(d => d.HelpRequest);

    /// <summary>Quem recebeu a doação (dono do pedido vinculado) ou administrador.</summary>
    private static bool CanReview(Donation donation, Requester requester) =>
        requester.IsAdmin || donation.HelpRequest?.IdUser == requester.UserId;

    public async Task<ServiceResult<PagedResult<Review>>> GetAllAsync(ReviewQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllReviews");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        var reviews = WithDonation().AsNoTracking();

        if (query.IdDonation.HasValue)
            reviews = reviews.Where(r => r.IdDonation == query.IdDonation.Value);

        if (query.Rating.HasValue)
            reviews = reviews.Where(r => r.Rating == query.Rating.Value);

        IOrderedQueryable<Review> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "rating" => reviews.OrderByDirection(r => r.Rating, query.Ascending),
            "id" => reviews.OrderByDirection(r => r.Id, query.Ascending),
            _ => reviews.OrderByDirection(r => r.ReviewDate, query.Ascending)
        };

        var page = await ordered.ThenBy(r => r.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<Review>>.Ok(page);
    }

    public async Task<ServiceResult<Review>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetReviewById");
        activity?.SetTag("review.id", id);

        var review = await WithDonation().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            _logger.LogWarning("Avaliação de id {ReviewId} não encontrada.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Avaliação não encontrada");
            return ServiceResult<Review>.NotFound($"Avaliação de id {id} não encontrada!");
        }

        return ServiceResult<Review>.Ok(review);
    }

    public async Task<ServiceResult<Review>> CreateAsync(ReviewRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("CreateReview");
        activity?.SetTag("donation.id", request.IdDonation);

        var donation = await _context.Donations
            .Include(d => d.HelpRequest)
            .FirstOrDefaultAsync(d => d.Id == request.IdDonation);

        if (donation == null)
            return ServiceResult<Review>.NotFound($"Doação de id {request.IdDonation} não encontrada!");

        if (!CanReview(donation, requester))
            return ServiceResult<Review>.Forbidden("Somente quem recebeu a doação pode avaliá-la.");

        if (donation.Status != DonationStatus.Delivered)
            return ServiceResult<Review>.Conflict("Só é possível avaliar doações entregues (Delivered).");

        if (await _context.Reviews.AnyAsync(r => r.IdDonation == request.IdDonation))
            return ServiceResult<Review>.Conflict("Esta doação já foi avaliada.");

        var review = new Review
        {
            IdDonation = donation.Id,
            Donation = donation,
            Rating = request.Rating,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ReviewDate = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Avaliação criada com sucesso. ReviewId: {ReviewId}, DonationId: {DonationId}",
            review.Id, donation.Id);

        _reviewsCreatedCounter.Add(1);
        activity?.SetTag("review.id", review.Id);

        return ServiceResult<Review>.Ok(review);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, ReviewUpdateRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("UpdateReview");
        activity?.SetTag("review.id", id);

        var review = await WithDonation().FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            _logger.LogWarning("Tentativa de atualizar avaliação inexistente. ReviewId: {ReviewId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Avaliação não encontrada");
            return ServiceResult<bool>.NotFound($"Avaliação de id {id} não encontrada!");
        }

        if (!CanReview(review.Donation, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para alterar esta avaliação.");

        review.Update(request.Rating, string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
        await _context.SaveChangesAsync();

        _logger.LogInformation("Avaliação {ReviewId} atualizada com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("DeleteReview");
        activity?.SetTag("review.id", id);

        var review = await WithDonation().FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            _logger.LogWarning("Tentativa de remover avaliação inexistente. ReviewId: {ReviewId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Avaliação não encontrada");
            return ServiceResult<bool>.NotFound($"Avaliação de id {id} não encontrada!");
        }

        if (!CanReview(review.Donation, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para remover esta avaliação.");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Avaliação {ReviewId} removida com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }
}