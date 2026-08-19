using Linear.Domain.Common;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.Update;

/// <summary>
/// Edita el contenido de un comentario propio.
/// </summary>
public sealed class UpdateCommentHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<CommentResponse>> HandleAsync(
        UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var context = await CommentAccess.RequireEditableAsync(
            teamAccess,
            currentUser,
            dbContext,
            request.Key,
            request.Identifier,
            request.CommentId,
            cancellationToken);

        if (context.IsFailure)
        {
            return Result.Failure<CommentResponse>(context.Error);
        }

        var (comment, userId, role) = context.Value;

        var updated = comment.UpdateContent(request.Content, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<CommentResponse>(updated.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await CommentResponseMapper.ToResponseAsync(
            comment, userId, role, dbContext, cancellationToken));
    }
}
