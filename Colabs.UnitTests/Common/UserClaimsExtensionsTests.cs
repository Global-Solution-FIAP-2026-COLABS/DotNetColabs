using System.Security.Claims;
using Colabs.Common;

namespace Colabs.UnitTests.Common;

public class UserClaimsExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test", nameType: "name", roleType: "role"));

    [Fact]
    public void GetUserId_ShouldReadTheSubClaim()
    {
        var principal = PrincipalWith(new Claim("sub", "42"));

        Assert.Equal(42, principal.GetUserId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public void GetUserId_WithMissingOrInvalidClaim_ShouldReturnNull(string? sub)
    {
        var principal = sub is null ? PrincipalWith() : PrincipalWith(new Claim("sub", sub));

        Assert.Null(principal.GetUserId());
    }

    [Fact]
    public void IsAdmin_ShouldDependOnTheRoleClaim()
    {
        Assert.True(PrincipalWith(new Claim("role", "ADMIN")).IsAdmin());
        Assert.False(PrincipalWith(new Claim("role", "USER")).IsAdmin());
        Assert.False(PrincipalWith().IsAdmin());
    }
}