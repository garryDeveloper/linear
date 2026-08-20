using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.CreateItem;

/// <summary>
/// <c>POST /api/teams/{key}/roadmaps/{roadmapId}/items</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CreateRoadmapItemEndpoint(CreateRoadmapItemHandler handler)
    : Endpoint<CreateRoadmapItemRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/roadmaps/{roadmapId}/items");
    }

    public override async Task HandleAsync(
        CreateRoadmapItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
