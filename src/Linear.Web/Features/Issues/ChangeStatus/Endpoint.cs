using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.ChangeStatus;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}/status</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ChangeIssueStatusEndpoint(ChangeIssueStatusHandler handler)
    : Endpoint<ChangeIssueStatusRequest, IssueResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}/status");
    }

    public override async Task HandleAsync(ChangeIssueStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
