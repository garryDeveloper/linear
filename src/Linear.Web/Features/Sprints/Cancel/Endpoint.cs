using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.Cancel;

/// <summary>
/// <c>POST /api/teams/{key}/sprints/{sprintId}/cancel</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CancelSprintEndpoint(CancelSprintHandler handler)
    : Endpoint<CancelSprintRequest, SprintResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/sprints/{sprintId}/cancel");
    }

    public override async Task HandleAsync(CancelSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
