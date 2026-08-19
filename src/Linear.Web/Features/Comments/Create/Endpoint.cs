using FastEndpoints;

using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Shared.Results;

namespace Linear.Web.Features.Comments.Create;

/// <summary>
/// <c>POST /api/teams/{key}/issues/{identifier}/comments</c> — requiere pertenecer al equipo.
/// </summary>
public sealed class CreateCommentEndpoint(CreateCommentHandler handler)
    : Endpoint<CreateCommentRequest, CommentResponse>
{
    public override void Configure()
    {
        Post("teams/{key}/issues/{identifier}/comments");
    }

    public override async Task HandleAsync(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        await Send.SendResultAsync(result, cancellationToken);
    }
}
