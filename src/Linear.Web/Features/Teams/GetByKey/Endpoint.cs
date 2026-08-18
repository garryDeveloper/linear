using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.GetByKey;

/// <summary>
/// <c>GET /api/teams/{key}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class GetTeamByKeyEndpoint(GetTeamByKeyHandler handler)
    : Endpoint<GetTeamByKeyRequest, TeamResponse>
{
    public override void Configure()
    {
        Get("teams/{key}");
    }

    public override async Task HandleAsync(GetTeamByKeyRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Key, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
