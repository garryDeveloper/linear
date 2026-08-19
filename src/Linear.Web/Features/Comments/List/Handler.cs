using Linear.Domain.Common;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.List;

/// <summary>
/// Lista los comentarios de un issue en orden cronológico.
/// </summary>
/// <remarks>
/// Del más viejo al más nuevo, al revés que el listado de issues: una conversación se lee
/// en el orden en que se escribió, y la página 1 es el principio del hilo.
/// </remarks>
public sealed class ListCommentsHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<CommentResponse>>> HandleAsync(
        ListCommentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var context = await CommentAccess.RequireIssueAsync(
            teamAccess, currentUser, dbContext, request.Key, request.Identifier, cancellationToken);

        if (context.IsFailure)
        {
            return Result.Failure<PagedResult<CommentResponse>>(context.Error);
        }

        var (issue, userId, role) = context.Value;

        var page = request.ToPageRequest();

        // Los eliminados no se listan: la eliminación es lógica para no romper la actividad
        // que los referencia, no para seguir mostrándolos.
        var query = dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.IssueId == issue.Id && comment.DeletedAt == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .OrderBy(comment => comment.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToArrayAsync(cancellationToken);

        var items = await CommentResponseMapper.ToResponsesAsync(
            comments, userId, role, dbContext, cancellationToken);

        return Result.Success(PagedResult<CommentResponse>.Create(items, page, totalCount));
    }
}
