using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.RemoveIssue;

/// <summary>
/// <c>DELETE /api/teams/{key}/sprints/{sprintId}/issues/{identifier}</c> — requiere
/// pertenecer al equipo.
/// </summary>
public sealed class RemoveSprintIssueEndpoint(RemoveSprintIssueHandler handler)
    : Endpoint<RemoveSprintIssueRequest, SprintResponse>
{
    public override void Configure()
    {
        Delete("teams/{key}/sprints/{sprintId}/issues/{identifier}");
    }

    public override async Task HandleAsync(RemoveSprintIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
