using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.Start;

/// <summary>
/// <c>POST /api/teams/{key}/sprints/{sprintId}/start</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class StartSprintEndpoint(StartSprintHandler handler)
    : Endpoint<StartSprintRequest, SprintResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/sprints/{sprintId}/start");
    }

    public override async Task HandleAsync(StartSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
