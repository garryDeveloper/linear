using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.ChangePriority;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}/priority</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ChangeIssuePriorityEndpoint(ChangeIssuePriorityHandler handler)
    : Endpoint<ChangeIssuePriorityRequest, IssueResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}/priority");
    }

    public override async Task HandleAsync(ChangeIssuePriorityRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
