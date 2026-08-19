using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Issues.Delete;

/// <summary>
/// <c>DELETE /api/teams/{key}/issues/{identifier}</c> — requiere rol Admin u Owner.
/// </summary>
public sealed class DeleteIssueEndpoint(DeleteIssueHandler handler) : Endpoint<DeleteIssueRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}/issues/{identifier}");
    }

    public override async Task HandleAsync(DeleteIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
