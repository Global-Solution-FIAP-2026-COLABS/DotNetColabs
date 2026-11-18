using System.Diagnostics;
using Colabs.Common;
using Colabs.Common.Validation;
using Colabs.Data;
using Colabs.Diagnostics;
using Colabs.Dtos;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Services;

public class UserService : IUserService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private const string ForbiddenMessage = "Você não tem permissão para acessar este usuário.";

    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Dono do cadastro ou administrador.</summary>
    private static bool CanAccess(int id, Requester requester) => requester.IsAdmin || requester.UserId == id;

    public async Task<ServiceResult<PagedResult<User>>> GetAllAsync(UserQueryParameters query)
    {
        using var activity = ActivitySource.StartActivity("GetAllUsers");
        activity?.SetTag("page.number", query.PageNumber);
        activity?.SetTag("page.size", query.PageSize);

        IQueryable<User> users = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            users = users.Where(u => u.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim().ToLower();
            users = users.Where(u => u.Email.Contains(email)); // e-mails são gravados em minúsculas
        }

        var cpf = CpfValidator.Normalize(query.Cpf);
        if (cpf.Length > 0)
        {
            users = users.Where(u => u.Cpf.Contains(cpf));
        }

        if (query.Role.HasValue)
        {
            var role = query.Role.Value;
            users = users.Where(u => u.Role == role);
        }

        IOrderedQueryable<User> ordered = query.SortBy?.Trim().ToLowerInvariant() switch
        {
            "email" => users.OrderByDirection(u => u.Email, query.Ascending),
            "id" => users.OrderByDirection(u => u.Id, query.Ascending),
            _ => users.OrderByDirection(u => u.Name, query.Ascending)
        };

        var page = await ordered.ThenBy(u => u.Id).ToPagedResultAsync(query);
        return ServiceResult<PagedResult<User>>.Ok(page);
    }

    public async Task<ServiceResult<User>> GetByIdAsync(int id, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("GetUserById");
        activity?.SetTag("user.id", id);

        // O acesso é checado antes da consulta: quem não pode ver o recurso também não descobre se ele existe.
        if (!CanAccess(id, requester))
        {
            _logger.LogWarning("Acesso negado ao usuário {UserId} pelo solicitante {RequesterId}.", id, requester.UserId);
            return ServiceResult<User>.Forbidden(ForbiddenMessage);
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            _logger.LogWarning("Usuário de id {UserId} não encontrado.", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Usuário não encontrado");
            return ServiceResult<User>.NotFound($"Usuário de id {id} não encontrado!");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<bool>> UpdateAsync(int id, UserUpdateRequest request, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("UpdateUser");
        activity?.SetTag("user.id", id);

        if (!CanAccess(id, requester))
        {
            _logger.LogWarning("Atualização negada do usuário {UserId} pelo solicitante {RequesterId}.", id, requester.UserId);
            return ServiceResult<bool>.Forbidden(ForbiddenMessage);
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue)
            return ServiceResult<bool>.ValidationError("Latitude e longitude devem ser informadas juntas.");

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Tentativa de atualizar usuário inexistente. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Usuário não encontrado");
            return ServiceResult<bool>.NotFound($"Usuário de id {id} não encontrado!");
        }

        var email = request.Email.NormalizeEmail();

        if (await _context.Users.AnyAsync(u => u.Id != id && u.Email == email))
        {
            _logger.LogWarning("Tentativa de usar e-mail já cadastrado na atualização. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "E-mail duplicado");
            return ServiceResult<bool>.Conflict("Já existe um usuário cadastrado com este e-mail.");
        }

        user.Update(request.Name.Trim(), request.Phone.Trim(), email, request.Address.Trim(),
                    request.Latitude, request.Longitude);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuário {UserId} atualizado com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("DeleteUser");
        activity?.SetTag("user.id", id);

        if (!CanAccess(id, requester))
        {
            _logger.LogWarning("Exclusão negada do usuário {UserId} pelo solicitante {RequesterId}.", id, requester.UserId);
            return ServiceResult<bool>.Forbidden(ForbiddenMessage);
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Tentativa de remover usuário inexistente. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Usuário não encontrado");
            return ServiceResult<bool>.NotFound($"Usuário de id {id} não encontrado!");
        }

        if (user.Role == UserRole.Admin && await _context.Users.CountAsync(u => u.Role == UserRole.Admin) <= 1)
        {
            _logger.LogWarning("Tentativa de remover o último administrador. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Último administrador");
            return ServiceResult<bool>.Conflict("Não é possível remover o último administrador do sistema.");
        }

        var hasLinks = await _context.HelpRequests.AnyAsync(h => h.IdUser == id)
                       || await _context.Donations.AnyAsync(d => d.IdUser == id);

        if (hasLinks)
        {
            _logger.LogWarning("Tentativa de remover usuário com vínculos. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Usuário com vínculos");
            return ServiceResult<bool>.Conflict("O usuário possui pedidos de ajuda ou doações vinculados e não pode ser removido.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuário {UserId} removido com sucesso.", id);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<User>> ChangeRoleAsync(int id, UserRole role, Requester requester)
    {
        using var activity = ActivitySource.StartActivity("ChangeUserRole");
        activity?.SetTag("user.id", id);

        // Defesa em profundidade: o Controller já restringe à policy AdminOnly.
        if (!requester.IsAdmin)
        {
            _logger.LogWarning("Alteração de papel negada ao solicitante {RequesterId}.", requester.UserId);
            return ServiceResult<User>.Forbidden("Somente administradores podem alterar papéis.");
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            _logger.LogWarning("Tentativa de alterar papel de usuário inexistente. UserId: {UserId}", id);
            activity?.SetStatus(ActivityStatusCode.Error, "Usuário não encontrado");
            return ServiceResult<User>.NotFound($"Usuário de id {id} não encontrado!");
        }

        // Idempotente: pedir o papel que o usuário já tem não é erro nem altera nada.
        if (user.Role == role)
            return ServiceResult<User>.Ok(user);

        if (requester.UserId == id && role != UserRole.Admin)
        {
            _logger.LogWarning("Administrador tentou rebaixar a si mesmo. UserId: {UserId}", id);
            return ServiceResult<User>.Conflict("Um administrador não pode rebaixar a si mesmo.");
        }

        if (user.Role == UserRole.Admin && role != UserRole.Admin
            && await _context.Users.CountAsync(u => u.Role == UserRole.Admin) <= 1)
        {
            return ServiceResult<User>.Conflict("Não é possível rebaixar o último administrador do sistema.");
        }

        user.Role = role;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Papel do usuário {UserId} alterado para {Role} pelo administrador {RequesterId}.",
            id, role, requester.UserId);

        return ServiceResult<User>.Ok(user);
    }
}