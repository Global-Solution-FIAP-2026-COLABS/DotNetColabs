using System.Diagnostics;
using System.Diagnostics.Metrics;
using Colabs.Common;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class HelpRequestService : IHelpRequestService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    // Transições de status permitidas: de qual status para quais.
    private static readonly Dictionary<HelpRequestStatus, HelpRequestStatus[]> AllowedTransitions = new()
    {
        [HelpRequestStatus.Open] = [HelpRequestStatus.InProgress, HelpRequestStatus.Canceled],
        [HelpRequestStatus.InProgress] = [HelpRequestStatus.Completed, HelpRequestStatus.Canceled],
        [HelpRequestStatus.Completed] = [],
        [HelpRequestStatus.Canceled] = []
    };

    private readonly AppDbContext _context;
    private readonly ILogger<HelpRequestService> _logger;
    private readonly Counter<int> _helpRequestsCreatedCounter;

    public HelpRequestService(AppDbContext context, ILogger<HelpRequestService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _helpRequestsCreatedCounter = meter.CreateCounter<int>("help_requests_created_total", description: "Total de pedidos de ajuda criados");
    }

    private IQueryable<HelpRequest> WithDetails() =>
        _context.HelpRequests.Include(h => h.Category).Include(h => h.Items);

    private static bool IsOwnerOrAdmin(HelpRequest request, Requester requester) =>
        requester.IsAdmin || request.IdUser == requester.UserId;

    public async Task<ServiceResult<PagedResult<HelpRequest>>> GetAllAsync(HelpRequestQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllHelpRequests");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        var requests = WithDetails().AsNoTracking();

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            requests = requests.Where(h => h.Status == status);
        }

        if (query.IdCategory.HasValue)
            requests = requests.Where(h => h.IdCategory == query.IdCategory.Value);

        if (query.IdUser.HasValue)
            requests = requests.Where(h => h.IdUser == query.IdUser.Value);

        if (!string.IsNullOrWhiteSpace(query.Description))
        {
            var text = query.Description.Trim().ToLower();
            requests = requests.Where(h => h.Description.ToLower().Contains(text));
        }

        IOrderedQueryable<HelpRequest> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "status" => requests.OrderByDirection(h => h.Status, query.Ascending),
            "id" => requests.OrderByDirection(h => h.Id, query.Ascending),
            _ => requests.OrderByDirection(h => h.CreationDate, query.Ascending)
        };

        var page = await ordered.ThenBy(h => h.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<HelpRequest>>.Ok(page);
    }

    public async Task<ServiceResult<HelpRequest>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetHelpRequestById");
        activity?.SetTag("helprequest.id", id);

        var request = await WithDetails().AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);

        if (request == null)
        {
            _logger.LogWarning("Pedido de ajuda de id {HelpRequestId} não encontrado.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Pedido não encontrado");
            return ServiceResult<HelpRequest>.NotFound($"Pedido de ajuda de id {id} não encontrado!");
        }

        return ServiceResult<HelpRequest>.Ok(request);
    }

    public async Task<ServiceResult<HelpRequest>> CreateAsync(HelpRequestRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("CreateHelpRequest");

        var category = await _context.Categories.FindAsync(request.IdCategory);
        if (category == null)
            return ServiceResult<HelpRequest>.ValidationError("Categoria inexistente.");

        if (request.IdInstitution.HasValue &&
            !await _context.Institutions.AnyAsync(i => i.Id == request.IdInstitution.Value))
            return ServiceResult<HelpRequest>.ValidationError("Instituição inexistente.");

        var helpRequest = new HelpRequest
        {
            IdCategory = request.IdCategory,
            Category = category,
            IdUser = requester.UserId, // o dono é sempre o usuário do token
            IdInstitution = request.IdInstitution,
            Description = request.Description.Trim(),
            Status = HelpRequestStatus.Open,
            CreationDate = DateTime.UtcNow,
            Items = request.Items
                .Select(i => new HelpRequestItem { Name = i.Name.Trim(), Quantity = i.Quantity })
                .ToList()
        };

        _context.HelpRequests.Add(helpRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido de ajuda criado com sucesso. HelpRequestId: {HelpRequestId}", helpRequest.Id);

        _helpRequestsCreatedCounter.Add(1);
        activity?.SetTag("helprequest.id", helpRequest.Id);

        return ServiceResult<HelpRequest>.Ok(helpRequest);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, HelpRequestRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("UpdateHelpRequest");
        activity?.SetTag("helprequest.id", id);

        var helpRequest = await WithDetails().FirstOrDefaultAsync(h => h.Id == id);

        if (helpRequest == null)
        {
            _logger.LogWarning("Tentativa de atualizar pedido inexistente. HelpRequestId: {HelpRequestId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Pedido não encontrado");
            return ServiceResult<bool>.NotFound($"Pedido de ajuda de id {id} não encontrado!");
        }

        if (!IsOwnerOrAdmin(helpRequest, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para alterar este pedido.");

        if (helpRequest.Status != HelpRequestStatus.Open)
            return ServiceResult<bool>.Conflict("Só é possível alterar pedidos com status Open.");

        if (!await _context.Categories.AnyAsync(c => c.Id == request.IdCategory))
            return ServiceResult<bool>.ValidationError("Categoria inexistente.");

        if (request.IdInstitution.HasValue &&
            !await _context.Institutions.AnyAsync(i => i.Id == request.IdInstitution.Value))
            return ServiceResult<bool>.ValidationError("Instituição inexistente.");

        helpRequest.Update(request.IdCategory, request.IdInstitution, request.Description.Trim());

        // Substitui a lista de itens: remove os antigos e adiciona os enviados.
        _context.HelpRequestItems.RemoveRange(helpRequest.Items);
        foreach (var item in request.Items)
            helpRequest.Items.Add(new HelpRequestItem { Name = item.Name.Trim(), Quantity = item.Quantity });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido de ajuda {HelpRequestId} atualizado com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<HelpRequest>> ChangeStatusAsync(int id, HelpRequestStatus status, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("ChangeHelpRequestStatus");
        activity?.SetTag("helprequest.id", id);

        var helpRequest = await WithDetails().FirstOrDefaultAsync(h => h.Id == id);

        if (helpRequest == null)
        {
            _logger.LogWarning("Tentativa de mudar status de pedido inexistente. HelpRequestId: {HelpRequestId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Pedido não encontrado");
            return ServiceResult<HelpRequest>.NotFound($"Pedido de ajuda de id {id} não encontrado!");
        }

        if (!IsOwnerOrAdmin(helpRequest, requester))
            return ServiceResult<HelpRequest>.Forbidden("Você não tem permissão para alterar este pedido.");

        if (!AllowedTransitions[helpRequest.Status].Contains(status))
        {
            _logger.LogWarning("Transição de status não permitida. HelpRequestId: {HelpRequestId}, {From} -> {To}",
                id, helpRequest.Status, status);
            return ServiceResult<HelpRequest>.Conflict(
                $"Não é possível mudar o status de {helpRequest.Status} para {status}.");
        }

        helpRequest.Status = status;

        if (status is HelpRequestStatus.Completed or HelpRequestStatus.Canceled)
            helpRequest.CompletionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido de ajuda {HelpRequestId} agora está {Status}.", id, status);

        return ServiceResult<HelpRequest>.Ok(helpRequest);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("DeleteHelpRequest");
        activity?.SetTag("helprequest.id", id);

        var helpRequest = await _context.HelpRequests.Include(h => h.Items).FirstOrDefaultAsync(h => h.Id == id);

        if (helpRequest == null)
        {
            _logger.LogWarning("Tentativa de remover pedido inexistente. HelpRequestId: {HelpRequestId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Pedido não encontrado");
            return ServiceResult<bool>.NotFound($"Pedido de ajuda de id {id} não encontrado!");
        }

        if (!IsOwnerOrAdmin(helpRequest, requester))
            return ServiceResult<bool>.Forbidden("Você não tem permissão para remover este pedido.");

        if (await _context.Donations.AnyAsync(d => d.IdHelpRequest == id))
            return ServiceResult<bool>.Conflict("O pedido possui doações vinculadas e não pode ser removido. Cancele-o em vez disso.");

        _context.HelpRequestItems.RemoveRange(helpRequest.Items);
        _context.HelpRequests.Remove(helpRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pedido de ajuda {HelpRequestId} removido com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }
}