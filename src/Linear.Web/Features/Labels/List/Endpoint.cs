using FastEndpoints;

using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Labels.List;

/// <summary>
/// <c>GET /api/teams/{key}/labels</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListLabelsEndpoint(ListLabelsHandler handler)
    : Endpoint<ListLabelsRequest, PagedResult<LabelResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/labels");
    }

    public override async Task HandleAsync(ListLabelsRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
