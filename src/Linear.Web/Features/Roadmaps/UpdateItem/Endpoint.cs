using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.UpdateItem;

/// <summary>
/// <c>PUT /api/teams/{key}/roadmaps/{roadmapId}/items/{itemId}</c> — requiere pertenecer al
/// equipo.
/// </summary>
public sealed class UpdateRoadmapItemEndpoint(UpdateRoadmapItemHandler handler)
    : Endpoint<UpdateRoadmapItemRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/roadmaps/{roadmapId}/items/{itemId}");
    }

    public override async Task HandleAsync(
        UpdateRoadmapItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
