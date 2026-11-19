using System.Diagnostics;
using System.Diagnostics.Metrics;
using Colabs.Common;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class DonationService : IDonationService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    // Transições manuais permitidas. Available -> Matched só acontece pelo endpoint de match.
    private static readonly Dictionary<DonationStatus, DonationStatus[]> AllowedTransitions = new()
    {
        [DonationStatus.Available] = [DonationStatus.Canceled],
        [DonationStatus.Matched] = [DonationStatus.Delivered, DonationStatus.Canceled],
        [DonationStatus.Delivered] = [],
        [DonationStatus.Canceled] = []
    };

    private readonly AppDbContext _context;
    private readonly ILogger<DonationService> _logger;
    private readonly Counter<int> _donationsCreatedCounter;

    public DonationService(AppDbContext context, ILogger<DonationService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _donationsCreatedCounter = meter.CreateCounter<int>("donations_created_total", description: "Total de doações criadas");
    }

    private IQueryable<Donation> WithDetails() =>
        _context.Donations.Include(d => d.Category).Include(d => d.Items);

    private static bool IsDonorOrAdmin(Donation donation, Requester requester) =>
        requester.IsAdmin || donation.IdUser == requester.UserId;

    public async Task<ServiceResult<PagedResult<Donation>>> GetAllAsync(DonationQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllDonations");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        var donations = WithDetails().AsNoTracking();

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            donations = donations.Where(d => d.Status == status);
        }

        if (query.IdCategory.HasValue)
            donations = donations.Where(d => d.IdCategory == query.IdCategory.Value);

        if (query.IdUser.HasValue)
            donations = donations.Where(d => d.IdUser == query.IdUser.Value);

        if (query.IdHelpRequest.HasValue)
            donations = donations.Where(d => d.IdHelpRequest == query.IdHelpRequest.Value);

        if (!string.IsNullOrWhiteSpace(query.Description))
        {
            var text = query.Description.Trim().ToLower();
            donations = donations.Where(d => d.Description.ToLower().Contains(text));
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

    public async Task<ServiceResult<Donation>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetDonationById");
        activity?.SetTag("donation.id", id);

        var donation = await WithDetails().AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null)
        {
            _logger.LogWarning("Doação de id {DonationId} não encontrada.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Doação não encontrada");
            return ServiceResult<Donation>.NotFound($"Doação de id {id} não encontrada!");
        }

        return ServiceResult<Donation>.Ok(donation);
    }

    public async Task<ServiceResult<Donation>> CreateAsync(DonationRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("CreateDonation");

        var category = await _context.Categories.FindAsync(request.IdCategory);
        if (category == null)
            return ServiceResult<Donation>.ValidationError("Categoria inexistente.");

        if (request.IdInstitution.HasValue &&
            !await _context.Institutions.AnyAsync(i => i.Id == request.IdInstitution.Value))
            return ServiceResult<Donation>.ValidationError("Instituição inexistente.");

        var donation = new Donation
        {
            IdCategory = request.IdCategory,
            Category = category,
            IdUser = requester.UserId, // o doador é sempre o usuário do token
            IdInstitution = request.IdInstitution,
            Description = request.Description.Trim(),
            Status = DonationStatus.Available,
            CreationDate = DateTime.UtcNow,
            Items = request.Items
                .Select(i => new DonationItem { Name = i.Name.Trim(), Quantity = i.Quantity })
                .ToList()
        };

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doação criada com sucesso. DonationId: {DonationId}", donation.Id);

        _donationsCreatedCounter.Add(1);
        activity?.SetTag("donation.id", donation.Id);

        return ServiceResult<Donation>.Ok(donation);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, DonationRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("UpdateDonation");
        activity?.SetTag("donation.id", id);

        var donation = await WithDetails().FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null)
        {
            _logger.LogWarning("Tentativa de atualizar doação inexistente. DonationId: {DonationId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Doação não encontrada");
            return ServiceResult<bool>.NotFound($"Doação de id {id} não encontrada!");
        }

        if (!IsDonorOrAdmin(donation, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para alterar esta doação.");

        if (donation.Status != DonationStatus.Available)
            return ServiceResult<bool>.Conflict("Só é possível alterar doações com status Available.");

        if (!await _context.Categories.AnyAsync(c => c.Id == request.IdCategory))
            return ServiceResult<bool>.ValidationError("Categoria inexistente.");

        if (request.IdInstitution.HasValue &&
            !await _context.Institutions.AnyAsync(i => i.Id == request.IdInstitution.Value))
            return ServiceResult<bool>.ValidationError("Instituição inexistente.");

        donation.Update(request.IdCategory, request.Description.Trim());
        donation.IdInstitution = request.IdInstitution;

        // Substitui a lista de itens: remove os antigos e adiciona os enviados.
        _context.DonationItems.RemoveRange(donation.Items);
        foreach (var item in request.Items)
            donation.Items.Add(new DonationItem { Name = item.Name.Trim(), Quantity = item.Quantity });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Doação {DonationId} atualizada com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<Donation>> ChangeStatusAsync(int id, DonationStatus status, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("ChangeDonationStatus");
        activity?.SetTag("donation.id", id);

        var donation = await WithDetails().Include(d => d.HelpRequest).FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null)
        {
            _logger.LogWarning("Tentativa de mudar status de doação inexistente. DonationId: {DonationId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Doação não encontrada");
            return ServiceResult<Donation>.NotFound($"Doação de id {id} não encontrada!");
        }

        // Doador e admin fazem qualquer transição; o dono do pedido vinculado também pode confirmar a entrega.
        var isRecipient = donation.HelpRequest?.IdUser == requester.UserId;
        var allowed = IsDonorOrAdmin(donation, requester) || (status == DonationStatus.Delivered && isRecipient);

        if (!allowed)
            return ServiceResult<Donation>.Forbidden("Você não tem permissão para alterar esta doação.");

        if (!AllowedTransitions[donation.Status].Contains(status))
        {
            _logger.LogWarning("Transição de status não permitida. DonationId: {DonationId}, {From} -> {To}",
                id, donation.Status, status);
            return ServiceResult<Donation>.Conflict(
                $"Não é possível mudar o status de {donation.Status} para {status}.");
        }

        donation.Status = status;
        donation.CompletionDate = DateTime.UtcNow; // Delivered e Canceled encerram a doação

        var helpRequest = donation.HelpRequest;
        if (helpRequest != null && helpRequest.Status == HelpRequestStatus.InProgress)
        {
            if (status == DonationStatus.Delivered)
            {
                // A entrega atende o pedido.
                helpRequest.Status = HelpRequestStatus.Completed;
                helpRequest.CompletionDate = DateTime.UtcNow;
            }
            else if (status == DonationStatus.Canceled)
            {
                // Se não sobrar outra doação ativa, o pedido volta a ficar aberto.
                var hasOtherActive = await _context.Donations.AnyAsync(d =>
                    d.IdHelpRequest == helpRequest.Id && d.Id != donation.Id &&
                    (d.Status == DonationStatus.Matched || d.Status == DonationStatus.Delivered));

                if (!hasOtherActive)
                    helpRequest.Status = HelpRequestStatus.Open;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Doação {DonationId} agora está {Status}.", id, status);

        return ServiceResult<Donation>.Ok(donation);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("DeleteDonation");
        activity?.SetTag("donation.id", id);

        var donation = await _context.Donations.Include(d => d.Items).FirstOrDefaultAsync(d => d.Id == id);

        if (donation == null)
        {
            _logger.LogWarning("Tentativa de remover doação inexistente. DonationId: {DonationId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Doação não encontrada");
            return ServiceResult<bool>.NotFound($"Doação de id {id} não encontrada!");
        }

        if (!IsDonorOrAdmin(donation, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para remover esta doação.");

        if (donation.Status is DonationStatus.Matched or DonationStatus.Delivered)
            return ServiceResult<bool>.Conflict("Doações Matched ou Delivered não podem ser removidas.");

        _context.DonationItems.RemoveRange(donation.Items);
        _context.Donations.Remove(donation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Doação {DonationId} removida com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }
}