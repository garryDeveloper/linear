using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.Create;

/// <summary>
/// <c>POST /api/teams/{key}/issues</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CreateIssueEndpoint(CreateIssueHandler handler)
    : Endpoint<CreateIssueRequest, IssueResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/issues");
    }

    public override async Task HandleAsync(CreateIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
