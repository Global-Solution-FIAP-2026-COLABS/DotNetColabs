using Colabs.Common;

namespace Colabs.UnitTests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 10, 10)]
    [InlineData(5, 0, 0)]
    public void TotalPages_ShouldRoundUp(int totalRecords, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>(new List<int>(), totalRecords, 1, pageSize);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void Map_ShouldConvertItemsAndKeepPagingData()
    {
        var result = new PagedResult<int>(new List<int> { 1, 2, 3 }, 23, 2, 3);

        var mapped = result.Map(n => $"item-{n}");

        Assert.Equal(new[] { "item-1", "item-2", "item-3" }, mapped.Items);
        Assert.Equal(23, mapped.TotalRecords);
        Assert.Equal(2, mapped.PageNumber);
        Assert.Equal(3, mapped.PageSize);
        Assert.Equal(8, mapped.TotalPages);
    }
}