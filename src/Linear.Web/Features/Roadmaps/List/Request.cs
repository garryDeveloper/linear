using Linear.Web.Shared.Pagination;

namespace Linear.Web.Features.Roadmaps.List;

public sealed class ListRoadmapsRequest
{
    public string Key { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public PageRequest ToPageRequest() => new() { Page = Page, PageSize = PageSize };
}
