using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.GetByIdentifier;

/// <summary>
/// <c>GET /api/teams/{key}/issues/{identifier}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class GetIssueByIdentifierEndpoint(GetIssueByIdentifierHandler handler)
    : Endpoint<GetIssueByIdentifierRequest, IssueResponse>
{
    public override void Configure()
    {
        Get("teams/{key}/issues/{identifier}");
    }

    public override async Task HandleAsync(GetIssueByIdentifierRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
