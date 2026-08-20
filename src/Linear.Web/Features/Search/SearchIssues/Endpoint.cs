using FastEndpoints;

using Linear.Web.Features.Search.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Search.SearchIssues;

/// <summary>
/// <c>GET /api/search/issues</c> — busca en los equipos del usuario autenticado.
/// </summary>
public sealed class SearchIssuesEndpoint(SearchIssuesHandler handler)
    : Endpoint<SearchIssuesRequest, IReadOnlyList<SearchResultResponse>>
{
    public override void Configure()
    {
        Get("search/issues");
    }

    public override async Task HandleAsync(SearchIssuesRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
