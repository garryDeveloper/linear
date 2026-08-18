using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.Update;

/// <summary>
/// <c>PUT /api/teams/{key}</c> — requiere rol Admin u Owner en el equipo.
/// </summary>
public sealed class UpdateTeamEndpoint(UpdateTeamHandler handler)
    : Endpoint<UpdateTeamRequest, TeamResponse>
{
    public override void Configure()
    {
        Put("teams/{key}");
    }

    public override async Task HandleAsync(UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
