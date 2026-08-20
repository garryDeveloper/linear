using FastEndpoints;

using Linear.Web.Features.Activities.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Activities.ListIssueActivity;

/// <summary>
/// <c>GET /api/teams/{key}/issues/{identifier}/activity</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListIssueActivityEndpoint(ListIssueActivityHandler handler)
    : Endpoint<ListIssueActivityRequest, PagedResult<ActivityResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/issues/{identifier}/activity");
    }

    public override async Task HandleAsync(
        ListIssueActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
