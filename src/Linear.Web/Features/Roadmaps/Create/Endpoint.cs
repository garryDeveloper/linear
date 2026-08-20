using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.Create;

/// <summary>
/// <c>POST /api/teams/{key}/roadmaps</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CreateRoadmapEndpoint(CreateRoadmapHandler handler)
    : Endpoint<CreateRoadmapRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/roadmaps");
    }

    public override async Task HandleAsync(CreateRoadmapRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
