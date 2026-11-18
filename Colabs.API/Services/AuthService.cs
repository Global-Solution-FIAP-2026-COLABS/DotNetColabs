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

public class AuthService : IAuthService
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ServiceName);

    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly Counter<int> _usersRegisteredCounter;
    private readonly Counter<int> _loginFailuresCounter;

    public AuthService(AppDbContext context, ITokenService tokenService, ILogger<AuthService> logger, IMeterFactory meterFactory)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;

        var meter = meterFactory.Create(TelemetryConstants.MeterName);
        _usersRegisteredCounter = meter.CreateCounter<int>("users_registered_total", description: "Total de usuários cadastrados");
        _loginFailuresCounter = meter.CreateCounter<int>("login_failures_total", description: "Total de tentativas de login com falha");
    }

    public async Task<ServiceResult<User>> RegisterAsync(RegisterRequest request)
    {
        using var activity = ActivitySource.StartActivity("RegisterUser");

        if (request.Latitude.HasValue != request.Longitude.HasValue)
            return ServiceResult<User>.ValidationError("Latitude e longitude devem ser informadas juntas.");

        // ToEntity normaliza CPF (só dígitos) e e-mail (minúsculas) e força o papel USER.
        var user = request.ToEntity();

        if (!CpfValidator.IsValid(user.Cpf))
            return ServiceResult<User>.ValidationError("CPF inválido.");

        if (await _context.Users.AnyAsync(u => u.Cpf == user.Cpf))
        {
            _logger.LogWarning("Tentativa de cadastro com CPF já existente.");
            activity?.SetStatus(ActivityStatusCode.Error, "CPF duplicado");
            return ServiceResult<User>.Conflict("Já existe um usuário cadastrado com este CPF.");
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            _logger.LogWarning("Tentativa de cadastro com e-mail já existente.");
            activity?.SetStatus(ActivityStatusCode.Error, "E-mail duplicado");
            return ServiceResult<User>.Conflict("Já existe um usuário cadastrado com este e-mail.");
        }

        // A senha nunca é gravada em texto puro: armazena-se somente o hash (ASP.NET Core Identity).
        user.Password = UserPasswordHasher.Hash(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Só o Id vai para o log: nome, e-mail e CPF são dados pessoais (LGPD).
        _logger.LogInformation("Usuário cadastrado com sucesso. UserId: {UserId}", user.Id);

        _usersRegisteredCounter.Add(1);
        activity?.SetTag("user.id", user.Id);

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        using var activity = ActivitySource.StartActivity("Login");

        var email = request.Email.NormalizeEmail();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        // Quando o e-mail não existe, ainda assim gasta o tempo de uma verificação de senha.
        var passwordOk = user is null
            ? UserPasswordHasher.VerifyDummy(request.Password)
            : UserPasswordHasher.Verify(user, request.Password);

        // Mesma resposta para e-mail inexistente e senha incorreta: não revela quais e-mails existem.
        if (user is null || !passwordOk)
        {
            _logger.LogWarning("Falha de autenticação: credenciais inválidas.");
            _loginFailuresCounter.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, "Credenciais inválidas");
            return ServiceResult<LoginResponse>.Unauthorized("E-mail ou senha inválidos.");
        }

        var token = _tokenService.GenerateToken(user);

        _logger.LogInformation("Login realizado com sucesso. UserId: {UserId}", user.Id);
        activity?.SetTag("user.id", user.Id);

        return ServiceResult<LoginResponse>.Ok(new LoginResponse(
            token.Token, "Bearer", token.ExpiresAt, user.Id, user.Name, user.Email, AuthRoles.From(user.Role)));
    }
}