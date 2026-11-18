using System.IdentityModel.Tokens.Jwt;
using Colabs.Infrastructure;
using Colabs.Models;
using Colabs.Services;
using Microsoft.Extensions.Options;

namespace Colabs.UnitTests.Services;

public class TokenServiceTests
{
    private static TokenService CreateService(int expirationMinutes = 60) =>
        new(Options.Create(new JwtSettings
        {
            Key = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres",
            Issuer = "Colabs.Tests",
            Audience = "Colabs.Tests.Clients",
            ExpirationMinutes = expirationMinutes
        }));

    private static string ClaimValue(JwtSecurityToken jwt, string type) =>
        jwt.Claims.Single(c => c.Type == type).Value;

    [Fact]
    public void GenerateToken_ShouldContainIdentityAndRoleClaims()
    {
        var user = new User { Id = 7, Name = "Maria", Email = "maria@example.com", Role = UserRole.Admin };

        var result = CreateService().GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("7", ClaimValue(jwt, "sub"));
        Assert.Equal("maria@example.com", ClaimValue(jwt, "email"));
        Assert.Equal("Maria", ClaimValue(jwt, "name"));
        Assert.Equal("ADMIN", ClaimValue(jwt, "role"));
        Assert.False(string.IsNullOrWhiteSpace(ClaimValue(jwt, "jti")));
        Assert.Equal("Colabs.Tests", jwt.Issuer);
        Assert.Contains("Colabs.Tests.Clients", jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_ForRegularUser_ShouldUseUserRole()
    {
        var user = new User { Id = 1, Name = "Joao", Email = "joao@example.com", Role = UserRole.User };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().GenerateToken(user).Token);

        Assert.Equal("USER", ClaimValue(jwt, "role"));
    }

    [Fact]
    public void GenerateToken_ShouldExpireAfterConfiguredMinutes()
    {
        var user = new User { Id = 1, Name = "Joao", Email = "joao@example.com" };

        var result = CreateService(expirationMinutes: 30).GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(DateTime.UtcNow.AddMinutes(30), result.ExpiresAt, TimeSpan.FromSeconds(5));
        Assert.Equal(result.ExpiresAt, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }
}