using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.AddIssue;

/// <summary>
/// <c>POST /api/teams/{key}/sprints/{sprintId}/issues/{identifier}</c> — requiere pertenecer
/// al equipo.
/// </summary>
public sealed class AddSprintIssueEndpoint(AddSprintIssueHandler handler)
    : Endpoint<AddSprintIssueRequest, SprintResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/sprints/{sprintId}/issues/{identifier}");
    }

    public override async Task HandleAsync(AddSprintIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
