using Linear.Web.Shared.Pagination;

namespace Linear.UnitTests.Shared;

public class PageRequestTests
{
    [Fact]
    public void Defaults_AreTheFirstPageWithTheDefaultSize()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.EffectivePage);
        Assert.Equal(PageRequest.DefaultPageSize, request.EffectivePageSize);
        Assert.Equal(0, request.Skip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PageBelowOne_IsNormalizedToTheFirstPage(int page)
    {
        var request = new PageRequest { Page = page };

        Assert.Equal(1, request.EffectivePage);
        Assert.Equal(0, request.Skip);
    }

    [Fact]
    public void PageSizeAboveTheMaximum_IsCapped()
    {
        var request = new PageRequest { PageSize = PageRequest.MaxPageSize + 500 };

        Assert.Equal(PageRequest.MaxPageSize, request.EffectivePageSize);
        Assert.Equal(PageRequest.MaxPageSize, request.Take);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSizeBelowOne_FallsBackToTheDefault(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        Assert.Equal(PageRequest.DefaultPageSize, request.EffectivePageSize);
    }

    [Fact]
    public void Skip_SkipsThePreviousPages()
    {
        var request = new PageRequest { Page = 3, PageSize = 20 };

        Assert.Equal(40, request.Skip);
        Assert.Equal(20, request.Take);
    }
}

public class PagedResultTests
{
    [Fact]
    public void TotalPages_RoundsUp()
    {
        var result = PagedResult<int>.Create([1, 2, 3], new PageRequest { PageSize = 3 }, totalCount: 7);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void FirstPage_HasNoPreviousPage()
    {
        var result = PagedResult<int>.Create([1], new PageRequest { Page = 1, PageSize = 1 }, totalCount: 3);

        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void LastPage_HasNoNextPage()
    {
        var result = PagedResult<int>.Create([3], new PageRequest { Page = 3, PageSize = 1 }, totalCount: 3);

        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Create_NormalizesThePageRequest()
    {
        var result = PagedResult<int>.Create([], new PageRequest { Page = -2, PageSize = 0 }, totalCount: 0);

        Assert.Equal(1, result.Page);
        Assert.Equal(PageRequest.DefaultPageSize, result.PageSize);
    }

    [Fact]
    public void Empty_HasNoItemsAndNoPages()
    {
        var result = PagedResult<int>.Empty(new PageRequest());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void Create_RejectsANegativeTotalCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PagedResult<int>.Create([], new PageRequest(), totalCount: -1));
    }
}
