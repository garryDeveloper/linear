using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.AssignUser;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}/assignee</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class AssignIssueUserEndpoint(AssignIssueUserHandler handler)
    : Endpoint<AssignIssueUserRequest, IssueResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}/assignee");
    }

    public override async Task HandleAsync(AssignIssueUserRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
