using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.DeleteItem;

/// <summary>
/// <c>DELETE /api/teams/{key}/roadmaps/{roadmapId}/items/{itemId}</c> — requiere rol Admin
/// u Owner.
/// </summary>
public sealed class DeleteRoadmapItemEndpoint(DeleteRoadmapItemHandler handler)
    : Endpoint<DeleteRoadmapItemRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}/roadmaps/{roadmapId}/items/{itemId}");
    }

    public override async Task HandleAsync(
        DeleteRoadmapItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
