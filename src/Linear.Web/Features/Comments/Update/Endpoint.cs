using FastEndpoints;

using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Comments.Update;

/// <summary>
/// <c>PUT /api/teams/{key}/issues/{identifier}/comments/{commentId}</c> — solo el autor.
/// </summary>
public sealed class UpdateCommentEndpoint(UpdateCommentHandler handler)
    : Endpoint<UpdateCommentRequest, CommentResponse>
{
    public override void Configure()
    {
        Put("teams/{key}/issues/{identifier}/comments/{commentId}");
    }

    public override async Task HandleAsync(UpdateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
