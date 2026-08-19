using Linear.Domain.Comments;
using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.Contracts;

/// <summary>
/// Arma las respuestas de la feature de Comments.
/// </summary>
/// <remarks>
/// Los autores se cargan de una sola consulta para toda la página, no uno por comentario:
/// una conversación larga escrita por tres personas no debería costar treinta consultas.
/// </remarks>
public static class CommentResponseMapper
{
    public static async Task<CommentResponse> ToResponseAsync(
        Comment comment,
        Guid currentUserId,
        TeamRole currentUserRole,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);

        var responses = await ToResponsesAsync(
            [comment], currentUserId, currentUserRole, dbContext, cancellationToken);

        return responses[0];
    }

    public static async Task<IReadOnlyList<CommentResponse>> ToResponsesAsync(
        IReadOnlyList<Comment> comments,
        Guid currentUserId,
        TeamRole currentUserRole,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comments);
        ArgumentNullException.ThrowIfNull(dbContext);

        if (comments.Count == 0)
        {
            return [];
        }

        var authorIds = comments.Select(comment => comment.AuthorId).ToHashSet();

        var authors = await dbContext.Users
            .AsNoTracking()
            .Where(user => authorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return comments
            .Select(comment => new CommentResponse(
                comment.Id,
                comment.Content,
                ToAuthorResponse(comment.AuthorId, authors),
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.IsEdited,
                CommentPermissions.CanEdit(comment, currentUserId),
                CommentPermissions.CanDelete(comment, currentUserId, currentUserRole)))
            .ToArray();
    }

    /// <summary>
    /// El autor siempre existe —la clave foránea es <c>Restrict</c>, así que una cuenta con
    /// comentarios no se puede borrar—, pero si alguna vez faltara, el comentario se muestra
    /// igual con un autor desconocido en vez de tumbar la conversación entera.
    /// </summary>
    private static IssueUserResponse ToAuthorResponse(
        Guid authorId,
        IReadOnlyDictionary<Guid, User> authors) =>
        authors.TryGetValue(authorId, out var author)
            ? new IssueUserResponse(author.Id, author.Name, author.AvatarUrl)
            : new IssueUserResponse(authorId, "Usuario desconocido", null);
}
