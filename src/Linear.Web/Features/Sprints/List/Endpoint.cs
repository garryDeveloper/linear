using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.List;

/// <summary>
/// <c>GET /api/teams/{key}/sprints</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListSprintsEndpoint(ListSprintsHandler handler)
    : Endpoint<ListSprintsRequest, PagedResult<SprintSummaryResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/sprints");
    }

    public override async Task HandleAsync(ListSprintsRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
