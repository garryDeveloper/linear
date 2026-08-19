using FastEndpoints;

using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.Update;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class UpdateIssueEndpoint(UpdateIssueHandler handler)
    : Endpoint<UpdateIssueRequest, IssueResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}");
    }

    public override async Task HandleAsync(UpdateIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
