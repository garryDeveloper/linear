using Linear.Domain.Comments;
using Linear.Domain.Common;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.Create;

/// <summary>
/// Publica un comentario en un issue.
/// </summary>
public sealed class CreateCommentHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<CommentResponse>> HandleAsync(
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var context = await CommentAccess.RequireIssueAsync(
            teamAccess, currentUser, dbContext, request.Key, request.Identifier, cancellationToken);

        if (context.IsFailure)
        {
            return Result.Failure<CommentResponse>(context.Error);
        }

        var (issue, userId, role) = context.Value;

        var comment = Comment.Create(issue.Id, userId, request.Content, DateTimeOffset.UtcNow);

        if (comment.IsFailure)
        {
            return Result.Failure<CommentResponse>(comment.Error);
        }

        dbContext.Comments.Add(comment.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await CommentResponseMapper.ToResponseAsync(
            comment.Value, userId, role, dbContext, cancellationToken));
    }
}
