using Colabs.Common;

namespace Colabs.UnitTests.Common;

public class QueryParametersTests
{
    [Fact]
    public void NewInstance_ShouldHaveDefaults()
    {
        var query = new QueryParameters();

        Assert.Equal(1, query.PageNumber);
        Assert.Equal(10, query.PageSize);
        Assert.Null(query.SortBy);
        Assert.True(query.Ascending);
    }

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void PageNumber_ShouldNeverBeLessThanOne(int input, int expected)
    {
        var query = new QueryParameters { PageNumber = input };

        Assert.Equal(expected, query.PageNumber);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 10)]
    [InlineData(25, 25)]
    [InlineData(50, 50)]
    [InlineData(500, 50)]
    public void PageSize_ShouldDefaultWhenInvalidAndBeCappedAtMax(int input, int expected)
    {
        var query = new QueryParameters { PageSize = input };

        Assert.Equal(expected, query.PageSize);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 20, 40)]
    public void Skip_ShouldBeCalculatedFromPageNumberAndSize(int page, int size, int expectedSkip)
    {
        var query = new QueryParameters { PageNumber = page, PageSize = size };

        Assert.Equal(expectedSkip, query.Skip);
    }
}