using Linear.Web.Shared.Pagination;

namespace Linear.Web.Features.Teams.List;

public sealed class ListTeamsRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public PageRequest ToPageRequest() => new() { Page = Page, PageSize = PageSize };
}
