using FastEndpoints;

using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Roadmaps.AddIssue;

/// <summary>
/// <c>POST /api/teams/{key}/roadmaps/{roadmapId}/items/{itemId}/issues/{identifier}</c> —
/// requiere pertenecer al equipo.
/// </summary>
public sealed class AddRoadmapItemIssueEndpoint(AddRoadmapItemIssueHandler handler)
    : Endpoint<AddRoadmapItemIssueRequest, RoadmapResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/roadmaps/{roadmapId}/items/{itemId}/issues/{identifier}");
    }

    public override async Task HandleAsync(
        AddRoadmapItemIssueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
