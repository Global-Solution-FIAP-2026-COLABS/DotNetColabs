using Colabs.Common;
using Microsoft.AspNetCore.Routing;

namespace Colabs.UnitTests.Common;

public class PaginationLinkBuilderTests
{
    // Gerador de URL falso: "/api/test?chave=valor&..." (ignora valores nulos, como o Url.Link real).
    private static string? FakeHref(RouteValueDictionary values) =>
        "/api/test?" + string.Join("&",
            values.Where(p => p.Value is not null)
                  .OrderBy(p => p.Key, StringComparer.Ordinal)
                  .Select(p => $"{p.Key}={p.Value}"));

    private static string[] Rels(List<LinkDto> links) => links.Select(l => l.Rel).ToArray();

    [Fact]
    public void FirstPageOfMany_ShouldHaveSelfNextAndLast()
    {
        var query = new QueryParameters { PageNumber = 1 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 3);

        Assert.Equal(new[] { "self", "next_page", "last_page" }, Rels(links));
    }

    [Fact]
    public void MiddlePage_ShouldHaveAllFiveLinks()
    {
        var query = new QueryParameters { PageNumber = 2 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 3);

        Assert.Equal(new[] { "self", "first_page", "prev_page", "next_page", "last_page" }, Rels(links));
        Assert.Contains("pageNumber=1", links.Single(l => l.Rel == "first_page").Href);
        Assert.Contains("pageNumber=1", links.Single(l => l.Rel == "prev_page").Href);
        Assert.Contains("pageNumber=3", links.Single(l => l.Rel == "next_page").Href);
        Assert.Contains("pageNumber=3", links.Single(l => l.Rel == "last_page").Href);
    }

    [Fact]
    public void LastPage_ShouldHaveSelfFirstAndPrev()
    {
        var query = new QueryParameters { PageNumber = 3 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 3);

        Assert.Equal(new[] { "self", "first_page", "prev_page" }, Rels(links));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NoRecordsOrSinglePage_ShouldHaveOnlySelf(int totalPages)
    {
        var query = new QueryParameters { PageNumber = 1 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages);

        Assert.Equal(new[] { "self" }, Rels(links));
    }

    [Fact]
    public void PageBeyondTheEnd_ShouldPointPrevToTheLastExistingPage()
    {
        var query = new QueryParameters { PageNumber = 9 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 3);

        Assert.Contains("pageNumber=3", links.Single(l => l.Rel == "prev_page").Href);
        Assert.DoesNotContain(links, l => l.Rel == "next_page");
    }

    [Fact]
    public void Filters_ShouldBeKeptInEveryLink_AndNullFiltersOmitted()
    {
        var query = new QueryParameters { PageNumber = 2, PageSize = 5, SortBy = "name", Ascending = false };
        var filters = new Dictionary<string, object?> { ["city"] = "sao-paulo", ["category"] = null };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 4, filters);

        Assert.All(links, l =>
        {
            Assert.Contains("city=sao-paulo", l.Href);
            Assert.Contains("pageSize=5", l.Href);
            Assert.Contains("sortBy=name", l.Href);
            Assert.Contains("ascending=False", l.Href);
            Assert.DoesNotContain("category=", l.Href);
        });
    }

    [Fact]
    public void CustomHttpMethod_ShouldBeAppliedToAllLinks()
    {
        var query = new QueryParameters { PageNumber = 2 };

        var links = PaginationLinkBuilder.Build(FakeHref, query, totalPages: 3, method: "POST");

        Assert.All(links, l => Assert.Equal("POST", l.Method));
    }
}