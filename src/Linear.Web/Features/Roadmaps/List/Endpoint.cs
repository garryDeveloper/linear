using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.List;

/// <summary>
/// <c>GET /api/teams/{key}/roadmaps</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListRoadmapsEndpoint(ListRoadmapsHandler handler)
    : Endpoint<ListRoadmapsRequest, PagedResult<RoadmapSummaryResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/roadmaps");
    }

    public override async Task HandleAsync(ListRoadmapsRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
