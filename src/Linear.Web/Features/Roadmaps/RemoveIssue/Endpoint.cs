using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.RemoveIssue;

/// <summary>
/// <c>DELETE /api/teams/{key}/roadmaps/{roadmapId}/items/{itemId}/issues/{identifier}</c> —
/// requiere pertenecer al equipo.
/// </summary>
public sealed class RemoveRoadmapItemIssueEndpoint(RemoveRoadmapItemIssueHandler handler)
    : Endpoint<RemoveRoadmapItemIssueRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Delete("teams/{key}/roadmaps/{roadmapId}/items/{itemId}/issues/{identifier}");
    }

    public override async Task HandleAsync(
        RemoveRoadmapItemIssueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
