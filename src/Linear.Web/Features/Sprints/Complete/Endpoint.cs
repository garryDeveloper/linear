using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.Complete;

/// <summary>
/// <c>POST /api/teams/{key}/sprints/{sprintId}/complete</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CompleteSprintEndpoint(CompleteSprintHandler handler)
    : Endpoint<CompleteSprintRequest, SprintResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/sprints/{sprintId}/complete");
    }

    public override async Task HandleAsync(CompleteSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
