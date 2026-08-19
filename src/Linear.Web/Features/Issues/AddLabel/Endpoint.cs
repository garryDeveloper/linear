using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.AddLabel;

/// <summary>
/// <c>POST /api/teams/{key}/issues/{identifier}/labels</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class AddIssueLabelEndpoint(AddIssueLabelHandler handler)
    : Endpoint<AddIssueLabelRequest, IssueResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/issues/{identifier}/labels");
    }

    public override async Task HandleAsync(AddIssueLabelRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
