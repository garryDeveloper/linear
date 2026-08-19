using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.Archive;

/// <summary>
/// <c>POST /api/teams/{key}/issues/{identifier}/archive</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class ArchiveIssueEndpoint(ArchiveIssueHandler handler)
    : Endpoint<ArchiveIssueRequest, IssueResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/issues/{identifier}/archive");
    }

    public override async Task HandleAsync(ArchiveIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
