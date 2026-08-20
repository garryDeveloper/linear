using FastEndpoints;

using Linear.Web.Features.Activities.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Activities.ListTeamActivity;

/// <summary>
/// <c>GET /api/teams/{key}/activity</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListTeamActivityEndpoint(ListTeamActivityHandler handler)
    : Endpoint<ListTeamActivityRequest, PagedResult<ActivityResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/activity");
    }

    public override async Task HandleAsync(
        ListTeamActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
