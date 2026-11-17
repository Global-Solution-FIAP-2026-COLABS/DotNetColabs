using Colabs.Common;

namespace Colabs.UnitTests.Common;

public class ServiceResultTests
{
    [Fact]
    public void Ok_ShouldCarryValueAndBeSuccess()
    {
        var result = ServiceResult<string>.Ok("valor");

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceStatus.Ok, result.Status);
        Assert.Equal("valor", result.Value);
        Assert.Null(result.Message);
    }

    [Theory]
    [InlineData(ServiceStatus.NotFound)]
    [InlineData(ServiceStatus.ValidationError)]
    [InlineData(ServiceStatus.Conflict)]
    [InlineData(ServiceStatus.Forbidden)]
    [InlineData(ServiceStatus.Unauthorized)]
    public void ErrorFactories_ShouldCarryMessageAndNotBeSuccess(ServiceStatus status)
    {
        var result = status switch
        {
            ServiceStatus.NotFound => ServiceResult<string>.NotFound("msg"),
            ServiceStatus.ValidationError => ServiceResult<string>.ValidationError("msg"),
            ServiceStatus.Conflict => ServiceResult<string>.Conflict("msg"),
            ServiceStatus.Forbidden => ServiceResult<string>.Forbidden("msg"),
            _ => ServiceResult<string>.Unauthorized("msg")
        };

        Assert.False(result.IsSuccess);
        Assert.Equal(status, result.Status);
        Assert.Equal("msg", result.Message);
        Assert.Null(result.Value);
    }
}