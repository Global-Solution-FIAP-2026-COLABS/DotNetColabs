using System.Diagnostics;
using System.Diagnostics.Metrics;
using Colabs.Common;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class CategoryService : ICategoryService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private readonly AppDbContext _context;
    private readonly ILogger<CategoryService> _logger;
    private readonly Counter<int> _categoriesCreatedCounter;

    public CategoryService(AppDbContext context, ILogger<CategoryService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _categoriesCreatedCounter = meter.CreateCounter<int>("categories_created_total", description: "Total de categorias cadastradas");
    }

    public async Task<ServiceResult<PagedResult<Category>>> GetAllAsync(CategoryQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllCategories");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        IQueryable<Category> categories = _context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            categories = categories.Where(c => c.Name.ToLower().Contains(name));
        }

        IOrderedQueryable<Category> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "id" => categories.OrderByDirection(c => c.Id, query.Ascending),
            _ => categories.OrderByDirection(c => c.Name, query.Ascending)
        };

        var page = await ordered.ThenBy(c => c.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<Category>>.Ok(page);
    }

    public async Task<ServiceResult<Category>> GetByIdAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetCategoryById");
        activity?.SetTag("category.id", id);

        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            _logger.LogWarning("Categoria de id {CategoryId} não encontrada.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Categoria não encontrada");
            return ServiceResult<Category>.NotFound($"Categoria de id {id} não encontrada!");
        }

        return ServiceResult<Category>.Ok(category);
    }

    public async Task<ServiceResult<Category>> CreateAsync(CategoryRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateCategory");

        var category = request.ToEntity();
        var normalized = category.Name.ToLower();

        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == normalized))
        {
            _logger.LogWarning("Tentativa de criar categoria com nome já existente.");
            activity?.SetStatus(ActivityStatusCode.Error, "Nome duplicado");
            return ServiceResult<Category>.Conflict($"Já existe uma categoria chamada '{category.Name}'.");
        }

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Categoria criada com sucesso. CategoryId: {CategoryId}", category.Id);

        _categoriesCreatedCounter.Add(1);
        activity?.SetTag("category.id", category.Id);

        return ServiceResult<Category>.Ok(category);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, CategoryRequest request)
    {
        using var activity = ActivitySource.StartActivity("UpdateCategory");
        activity?.SetTag("category.id", id);

        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            _logger.LogWarning("Tentativa de atualizar categoria inexistente. CategoryId: {CategoryId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Categoria não encontrada");
            return ServiceResult<bool>.NotFound($"Categoria de id {id} não encontrada!");
        }

        var name = request.Name.Trim();
        var normalized = name.ToLower();

        if (await _context.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == normalized))
        {
            _logger.LogWarning("Tentativa de renomear categoria para nome já existente. CategoryId: {CategoryId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Nome duplicado");
            return ServiceResult<bool>.Conflict($"Já existe uma categoria chamada '{name}'.");
        }

        category.Update(name);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Categoria {CategoryId} atualizada com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("DeleteCategory");
        activity?.SetTag("category.id", id);

        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            _logger.LogWarning("Tentativa de remover categoria inexistente. CategoryId: {CategoryId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Categoria não encontrada");
            return ServiceResult<bool>.NotFound($"Categoria de id {id} não encontrada!");
        }

        var inUse = await _context.HelpRequests.AnyAsync(h => h.IdCategory == id)
                    || await _context.Donations.AnyAsync(d => d.IdCategory == id);

        if (inUse)
        {
            _logger.LogWarning("Tentativa de remover categoria em uso. CategoryId: {CategoryId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Categoria em uso");
            return ServiceResult<bool>.Conflict("A categoria está em uso por pedidos de ajuda ou doações e não pode ser removida.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Categoria {CategoryId} removida com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }
}