using System.Diagnostics;
using System.Diagnostics.Metrics;
using Colabs.Common;
using Colabs.Common.Validation;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class InstitutionService : IInstitutionService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private readonly AppDbContext _context;
    private readonly ILogger<InstitutionService> _logger;
    private readonly Counter<int> _institutionsCreatedCounter;

    public InstitutionService(AppDbContext context, ILogger<InstitutionService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _institutionsCreatedCounter = meter.CreateCounter<int>("institutions_created_total", description: "Total de instituições cadastradas");
    }

    public async Task<ServiceResult<PagedResult<Institution>>> GetAllAsync(InstitutionQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllInstitutions");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        IQueryable<Institution> institutions = _context.Institutions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            institutions = institutions.Where(i => i.Name.ToLower().Contains(name));
        }

        // O CNPJ é gravado só com dígitos; o filtro aceita o texto com ou sem pontuação.
        var cnpj = CnpjValidator.Normalize(query.Cnpj);
        if (cnpj.Length > 0)
        {
            institutions = institutions.Where(i => i.Cnpj.Contains(cnpj));
        }

        if (!string.IsNullOrWhiteSpace(query.Address))
        {
            var address = query.Address.Trim().ToLower();
            institutions = institutions.Where(i => i.Address.ToLower().Contains(address));
        }

        IOrderedQueryable<Institution> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "cnpj" => institutions.OrderByDirection(i => i.Cnpj, query.Ascending),
            "address" => institutions.OrderByDirection(i => i.Address, query.Ascending),
            _ => institutions.OrderByDirection(i => i.Name, query.Ascending)
        };

        var page = await ordered.ThenBy(i => i.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<Institution>>.Ok(page);
    }

    public async Task<ServiceResult<Institution>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetInstitutionById");
        activity?.SetTag("institution.id", id);

        var institution = await _context.Institutions.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

        if (institution == null)
        {
            _logger.LogWarning("Instituição de id {InstitutionId} não encontrada.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Instituição não encontrada");
            return ServiceResult<Institution>.NotFound($"Instituição de id {id} não encontrada!");
        }

        return ServiceResult<Institution>.Ok(institution);
    }

    public async Task<ServiceResult<Institution>> CreateAsync(InstitutionRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateInstitution");

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return ServiceResult<Institution>.ValidationError("Latitude e longitude são obrigatórias.");

        var institution = request.ToEntity();

        if (!CnpjValidator.IsValid(institution.Cnpj))
            return ServiceResult<Institution>.ValidationError("CNPJ inválido.");

        if (await _context.Institutions.AnyAsync(i => i.Cnpj == institution.Cnpj))
        {
            _logger.LogWarning("Tentativa de criar instituição com CNPJ já existente.");
            activity?.SetStatus(ActivityStatusCode.Error, "CNPJ duplicado");
            return ServiceResult<Institution>.Conflict("Já existe uma instituição cadastrada com este CNPJ.");
        }

        _context.Institutions.Add(institution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Instituição criada com sucesso. InstitutionId: {InstitutionId}", institution.Id);

        _institutionsCreatedCounter.Add(1);
        activity?.SetTag("institution.id", institution.Id);

        return ServiceResult<Institution>.Ok(institution);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, InstitutionUpdateRequest request)
    {
        using var activity = ActivitySource.StartActivity("UpdateInstitution");
        activity?.SetTag("institution.id", id);

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return ServiceResult<bool>.ValidationError("Latitude e longitude são obrigatórias.");

        var institution = await _context.Institutions.FindAsync(id);

        if (institution == null)
        {
            _logger.LogWarning("Tentativa de atualizar instituição inexistente. InstitutionId: {InstitutionId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Instituição não encontrada");
            return ServiceResult<bool>.NotFound($"Instituição de id {id} não encontrada!");
        }

        institution.Update(request.Name.Trim(), request.Address.Trim(), request.Latitude.Value, request.Longitude.Value);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Instituição {InstitutionId} atualizada com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("DeleteInstitution");
        activity?.SetTag("institution.id", id);

        var institution = await _context.Institutions.FindAsync(id);

        if (institution == null)
        {
            _logger.LogWarning("Tentativa de remover instituição inexistente. InstitutionId: {InstitutionId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Instituição não encontrada");
            return ServiceResult<bool>.NotFound($"Instituição de id {id} não encontrada!");
        }

        var inUse = await _context.HelpRequests.AnyAsync(h => h.IdInstitution == id)
                    || await _context.Donations.AnyAsync(d => d.IdInstitution == id);

        if (inUse)
        {
            _logger.LogWarning("Tentativa de remover instituição com vínculos. InstitutionId: {InstitutionId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Instituição com vínculos");
            return ServiceResult<bool>.Conflict("A instituição possui pedidos de ajuda ou doações vinculados e não pode ser removida.");
        }

        _context.Institutions.Remove(institution);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Instituição {InstitutionId} removida com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }
}