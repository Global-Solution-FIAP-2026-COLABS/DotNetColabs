using System.Diagnostics;
using Colabs.Common;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class HistoryService : IHistoryService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private readonly AppDbContext _context;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(AppDbContext context, ILogger<HistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<Donation>>> GetDonationsAsync(HistoryQueryParameters query, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("GetDonationHistory");

        var userId = query.IdUser ?? requester.UserId;
        activity?.SetTag("user.id", userId);

        if (userId != requester.UserId && !requester.IsAdmin)
        {
            _logger.LogWarning("Acesso negado ao histórico do usuário {UserId} pelo solicitante {RequesterId}.",
                userId, requester.UserId);
            return ServiceResult<PagedResult<Donation>>.Forbidden("Você não tem permissão para ver o histórico de outro usuário.");
        }

        var donations = _context.Donations
            .Include(d => d.Category)
            .Include(d => d.Items)
            .Include(d => d.Reviews)
            .Include(d => d.HelpRequest)
            .AsNoTracking();

        // Doador: o usuário registrou a doação. Recebedor: o usuário é o dono do pedido vinculado.
        donations = query.Role switch
        {
            HistoryRole.Donor => donations.Where(d => d.IdUser == userId),
            HistoryRole.Recipient => donations.Where(d => d.HelpRequest != null && d.HelpRequest.IdUser == userId),
            _ => donations.Where(d => d.IdUser == userId || (d.HelpRequest != null && d.HelpRequest.IdUser == userId))
        };

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            donations = donations.Where(d => d.Status == status);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value.Date;
            donations = donations.Where(d => d.CreationDate >= from);
        }

        if (query.To.HasValue)
        {
            var toExclusive = query.To.Value.Date.AddDays(1);
            donations = donations.Where(d => d.CreationDate < toExclusive);
        }

        IOrderedQueryable<Donation> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "status" => donations.OrderByDirection(d => d.Status, query.Ascending),
            "id" => donations.OrderByDirection(d => d.Id, query.Ascending),
            _ => donations.OrderByDirection(d => d.CreationDate, query.Ascending)
        };

        var page = await ordered.ThenBy(d => d.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<Donation>>.Ok(page);
    }
}