using FastEndpoints;

using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Comments.List;

/// <summary>
/// <c>GET /api/teams/{key}/issues/{identifier}/comments</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ListCommentsEndpoint(ListCommentsHandler handler)
    : Endpoint<ListCommentsRequest, PagedResult<CommentResponse>>
{
    public override void Configure()
    {
        Get("teams/{key}/issues/{identifier}/comments");
    }

    public override async Task HandleAsync(ListCommentsRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
