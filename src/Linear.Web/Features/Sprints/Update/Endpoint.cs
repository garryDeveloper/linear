using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.Update;

/// <summary>
/// <c>PUT /api/teams/{key}/sprints/{sprintId}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class UpdateSprintEndpoint(UpdateSprintHandler handler)
    : Endpoint<UpdateSprintRequest, SprintResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/sprints/{sprintId}");
    }

    public override async Task HandleAsync(UpdateSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
