using Colabs.Common;
using Colabs.Models;

namespace Colabs.UnitTests.Common;

public class UserPasswordHasherTests
{
    [Fact]
    public void Hash_ShouldNotStorePlainTextAndShouldVerify()
    {
        var user = new User();

        user.Password = UserPasswordHasher.Hash(user, "SenhaForte123");

        Assert.NotEqual("SenhaForte123", user.Password);
        Assert.True(user.Password.Length <= 300); // cabe na coluna PASSWORD
        Assert.True(UserPasswordHasher.Verify(user, "SenhaForte123"));
    }

    [Fact]
    public void Hash_ShouldBeDifferentForTheSamePassword()
    {
        var user = new User();

        var first = UserPasswordHasher.Hash(user, "SenhaForte123");
        var second = UserPasswordHasher.Hash(user, "SenhaForte123");

        Assert.NotEqual(first, second); // sal aleatório
    }

    [Theory]
    [InlineData("senhaerrada")]
    [InlineData("")]
    public void Verify_WithWrongOrEmptyPassword_ShouldReturnFalse(string attempt)
    {
        var user = new User();
        user.Password = UserPasswordHasher.Hash(user, "SenhaForte123");

        Assert.False(UserPasswordHasher.Verify(user, attempt));
    }

    [Fact]
    public void Verify_WithStoredPlainText_ShouldReturnFalseWithoutThrowing()
    {
        var user = new User { Password = "senha-em-texto-puro" };

        Assert.False(UserPasswordHasher.Verify(user, "senha-em-texto-puro"));
    }

    [Fact]
    public void VerifyDummy_ShouldAlwaysReturnFalse()
    {
        Assert.False(UserPasswordHasher.VerifyDummy("qualquer-coisa"));
    }
}