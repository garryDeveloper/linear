using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.List;

/// <summary>
/// <c>GET /api/teams/{key}/issues</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListIssuesEndpoint(ListIssuesHandler handler)
    : Endpoint<ListIssuesRequest, PagedResult<IssueSummaryResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/issues");
    }

    public override async Task HandleAsync(ListIssuesRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
