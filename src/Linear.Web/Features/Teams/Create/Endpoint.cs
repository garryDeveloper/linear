using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.Create;

/// <summary>
/// <c>POST /api/teams</c> — cualquier usuario autenticado puede crear un equipo.
/// </summary>
public sealed class CreateTeamEndpoint(CreateTeamHandler handler)
    : Endpoint<CreateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Post("teams");
    }

    public override async Task HandleAsync(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
