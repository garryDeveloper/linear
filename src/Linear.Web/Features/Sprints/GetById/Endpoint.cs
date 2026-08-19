using FastEndpoints;

using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Sprints.GetById;

/// <summary>
/// <c>GET /api/teams/{key}/sprints/{sprintId}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class GetSprintByIdEndpoint(GetSprintByIdHandler handler)
    : Endpoint<GetSprintByIdRequest, SprintResponse>
{
    public override void Configure()
    {
        Get("teams/{key}/sprints/{sprintId}");
    }

    public override async Task HandleAsync(GetSprintByIdRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
