using FastEndpoints;

using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Teams.List;

/// <summary>
/// <c>GET /api/teams</c> — los equipos del usuario autenticado.
/// </summary>
public sealed class ListTeamsEndpoint(ListTeamsHandler handler)
    : Endpoint<ListTeamsRequest, PagedResult<TeamSummaryResponse>>
{
    public override void Configure()
    {
        Get("teams");
    }

    public override async Task HandleAsync(ListTeamsRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
