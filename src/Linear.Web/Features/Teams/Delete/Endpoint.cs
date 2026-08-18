using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.Delete;

/// <summary>
/// <c>DELETE /api/teams/{key}</c> — requiere rol Owner en el equipo.
/// </summary>
public sealed class DeleteTeamEndpoint(DeleteTeamHandler handler)
    : Endpoint<DeleteTeamRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}");
    }

    public override async Task HandleAsync(DeleteTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Key, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
