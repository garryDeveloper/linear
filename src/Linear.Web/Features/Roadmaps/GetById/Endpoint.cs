using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.GetById;

/// <summary>
/// <c>GET /api/teams/{key}/roadmaps/{roadmapId}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class GetRoadmapByIdEndpoint(GetRoadmapByIdHandler handler)
    : Endpoint<GetRoadmapByIdRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Get("teams/{key}/roadmaps/{roadmapId}");
    }

    public override async Task HandleAsync(GetRoadmapByIdRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
