using Colabs.Common;
using Colabs.Data;
using Colabs.Dtos;
using Colabs.Infrastructure;
using Colabs.Models;
using Colabs.Services;
using Colabs.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Colabs.UnitTests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(AppDbContext context)
    {
        var tokenService = new TokenService(Options.Create(new JwtSettings
        {
            Key = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres",
            Issuer = "Colabs.Tests",
            Audience = "Colabs.Tests.Clients",
            ExpirationMinutes = 60
        }));

        return new AuthService(context, tokenService, NullLogger<AuthService>.Instance, FakeMeterFactory.Create());
    }

    private static RegisterRequest NewRegister(
        string cpf = "529.982.247-25",
        string email = "Maria@Example.com",
        decimal? latitude = null,
        decimal? longitude = null) =>
        new("  Maria Silva ", cpf, "(11) 99999-0000", email, "SenhaForte123", "Rua das Flores, 100 - São Paulo", latitude, longitude);

    [Fact]
    public async Task Register_ShouldPersistNormalizedUserWithHashedPasswordAndUserRole()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegister());

        Assert.True(result.IsSuccess);
        var saved = await context.Users.SingleAsync();
        Assert.Equal("Maria Silva", saved.Name);
        Assert.Equal("52998224725", saved.Cpf);
        Assert.Equal("maria@example.com", saved.Email);
        Assert.Equal(UserRole.User, saved.Role);
        Assert.NotEqual("SenhaForte123", saved.Password);
        Assert.True(UserPasswordHasher.Verify(saved, "SenhaForte123"));
    }

    [Fact]
    public async Task Register_WithInvalidCpf_ShouldReturnValidationError()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegister(cpf: "111.111.111-11"));

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Empty(context.Users);
    }

    [Theory]
    [InlineData(-23.55, null)]
    [InlineData(null, -46.63)]
    public async Task Register_WithOnlyOneCoordinate_ShouldReturnValidationError(double? lat, double? lon)
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegister(
            latitude: lat.HasValue ? (decimal)lat.Value : null,
            longitude: lon.HasValue ? (decimal)lon.Value : null));

        Assert.Equal(ServiceStatus.ValidationError, result.Status);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task Register_WithBothCoordinates_ShouldPersistThem()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);

        var result = await service.RegisterAsync(NewRegister(latitude: -23.550520m, longitude: -46.633308m));

        Assert.True(result.IsSuccess);
        var saved = await context.Users.SingleAsync();
        Assert.Equal(-23.550520m, saved.Latitude);
        Assert.Equal(-46.633308m, saved.Longitude);
    }

    [Fact]
    public async Task Register_WithExistingCpfInDifferentFormat_ShouldReturnConflict()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegister(cpf: "529.982.247-25", email: "a@example.com"));

        var result = await service.RegisterAsync(NewRegister(cpf: "52998224725", email: "b@example.com"));

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task Register_WithExistingEmailIgnoringCase_ShouldReturnConflict()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegister(cpf: "529.982.247-25", email: "maria@example.com"));

        var result = await service.RegisterAsync(NewRegister(cpf: "111.444.777-35", email: "MARIA@EXAMPLE.COM"));

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ShouldReturnTokenIgnoringEmailCase()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegister());

        var result = await service.LoginAsync(new LoginRequest("MARIA@example.com", "SenhaForte123"));

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Token));
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal("USER", result.Value.Role);
        Assert.Equal("maria@example.com", result.Value.Email);
        Assert.True(result.Value.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegister());

        var result = await service.LoginAsync(new LoginRequest("maria@example.com", "senha-errada"));

        Assert.Equal(ServiceStatus.Unauthorized, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnTheSameUnauthorizedMessageAsWrongPassword()
    {
        using var context = InMemoryDbContextFactory.Create();
        var service = CreateService(context);
        await service.RegisterAsync(NewRegister());

        var unknown = await service.LoginAsync(new LoginRequest("ninguem@example.com", "SenhaForte123"));
        var wrongPassword = await service.LoginAsync(new LoginRequest("maria@example.com", "senha-errada"));

        Assert.Equal(ServiceStatus.Unauthorized, unknown.Status);
        Assert.Equal(wrongPassword.Message, unknown.Message); // não revela se o e-mail existe
    }
}