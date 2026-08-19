using FastEndpoints;

using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Comments.Delete;

/// <summary>
/// <c>DELETE /api/teams/{key}/issues/{identifier}/comments/{commentId}</c> — el autor, o un
/// Admin u Owner del equipo.
/// </summary>
public sealed class DeleteCommentEndpoint(DeleteCommentHandler handler) : Endpoint<DeleteCommentRequest>
{
    public override void Configure()
    {
        Delete("teams/{key}/issues/{identifier}/comments/{commentId}");
    }

    public override async Task HandleAsync(DeleteCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
