using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.RemoveLabel;

/// <summary>
/// <c>DELETE /api/teams/{key}/issues/{identifier}/labels/{labelId}</c> — requiere pertenecer
/// al equipo.
/// </summary>
public sealed class RemoveIssueLabelEndpoint(RemoveIssueLabelHandler handler)
    : Endpoint<RemoveIssueLabelRequest, IssueResponse>
{
    public override void Configure()
    {
        Delete("teams/{key}/issues/{identifier}/labels/{labelId}");
    }

    public override async Task HandleAsync(RemoveIssueLabelRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
