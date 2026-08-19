using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.Create;

/// <summary>
/// <c>POST /api/teams/{key}/sprints</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CreateSprintEndpoint(CreateSprintHandler handler)
    : Endpoint<CreateSprintRequest, SprintResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/sprints");
    }

    public override async Task HandleAsync(CreateSprintRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
