using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.ChangeEstimate;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}/estimate</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ChangeIssueEstimateEndpoint(ChangeIssueEstimateHandler handler)
    : Endpoint<ChangeIssueEstimateRequest, IssueResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}/estimate");
    }

    public override async Task HandleAsync(ChangeIssueEstimateRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
