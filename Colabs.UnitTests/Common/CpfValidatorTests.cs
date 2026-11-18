using Colabs.Common.Validation;

namespace Colabs.UnitTests.Common;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void IsValid_WithValidCpf_ShouldReturnTrue(string cpf)
    {
        Assert.True(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("529.982.247-26")]   // dígito verificador errado
    [InlineData("111.111.111-11")]   // sequência repetida
    [InlineData("123")]              // curto demais
    [InlineData("529.982.247-250")]  // longo demais
    [InlineData("abc.def.ghi-jk")]   // sem dígitos
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_WithInvalidCpf_ShouldReturnFalse(string? cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 529 982 247 25 ", "52998224725")]
    [InlineData(null, "")]
    public void Normalize_ShouldKeepOnlyDigits(string? input, string expected)
    {
        Assert.Equal(expected, CpfValidator.Normalize(input));
    }

    [Fact]
    public void CpfAttribute_ShouldIgnoreEmptyValuesAndRejectInvalidOnes()
    {
        var attribute = new CpfAttribute();

        Assert.True(attribute.IsValid(null));
        Assert.True(attribute.IsValid(""));
        Assert.True(attribute.IsValid("529.982.247-25"));
        Assert.False(attribute.IsValid("111.111.111-11"));
    }
}