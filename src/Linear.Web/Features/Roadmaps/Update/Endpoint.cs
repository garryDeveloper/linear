using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.Update;

/// <summary>
/// <c>PUT /api/teams/{key}/roadmaps/{roadmapId}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class UpdateRoadmapEndpoint(UpdateRoadmapHandler handler)
    : Endpoint<UpdateRoadmapRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/roadmaps/{roadmapId}");
    }

    public override async Task HandleAsync(UpdateRoadmapRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
