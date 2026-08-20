using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.Delete;

/// <summary>
/// <c>DELETE /api/teams/{key}/roadmaps/{roadmapId}</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class DeleteRoadmapEndpoint(DeleteRoadmapHandler handler) : Endpoint<DeleteRoadmapRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}/roadmaps/{roadmapId}");
    }

    public override async Task HandleAsync(DeleteRoadmapRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
