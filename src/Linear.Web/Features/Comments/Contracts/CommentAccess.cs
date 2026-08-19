using Linear.Domain.Comments;
using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Domain.Teams;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.Contracts;

/// <summary>
/// Resuelve el issue —y, cuando hace falta, el comentario— que identifica la ruta, junto
/// con quién está operando y con qué rol.
/// </summary>
/// <remarks>
/// El orden importa: primero el equipo y el issue, con <see cref="TeamIssueAccess"/>, que
/// ya responde 404 sin distinguir "no existe" de "no tenés acceso"; recién después se busca
/// el comentario, acotado a ese issue ya autorizado. Pedir un comentario de otro issue —o
/// de otro equipo— por la ruta equivocada nunca lo encuentra.
///
/// El rol viaja junto al resultado porque lo necesitan tanto la autorización de moderación
/// como <see cref="CommentResponseMapper"/> para calcular los permisos que devuelve cada
/// comentario. Sacarlo del equipo ya resuelto evita volver a consultarlo.
/// </remarks>
public static class CommentAccess
{
    /// <summary>
    /// Para crear y listar: alcanza con pertenecer al equipo dueño del issue.
    /// </summary>
    public static async Task<Result<(Issue Issue, Guid UserId, TeamRole Role)>> RequireIssueAsync(
        ITeamAccess teamAccess,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        var resolved = await TeamIssueAccess.RequireMemberAsync(
            teamAccess, dbContext, teamKey, identifier, trackIssue: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<(Issue, Guid, TeamRole)>(resolved.Error);
        }

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<(Issue, Guid, TeamRole)>(userId.Error);
        }

        // El acceso ya quedó verificado arriba, así que el rol existe: RequireMemberAsync
        // solo devuelve el equipo cuando quien pide es miembro.
        var role = resolved.Value.Team.RoleOf(userId.Value)!.Value;

        return Result.Success((resolved.Value.Issue, userId.Value, role));
    }

    /// <summary>Para editar: solo el autor.</summary>
    public static Task<Result<(Comment Comment, Guid UserId, TeamRole Role)>> RequireEditableAsync(
        ITeamAccess teamAccess,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        Guid commentId,
        CancellationToken cancellationToken) =>
        ResolveCommentAsync(
            teamAccess,
            currentUser,
            dbContext,
            teamKey,
            identifier,
            commentId,
            (comment, userId, _) => CommentPermissions.CanEdit(comment, userId),
            CommentErrors.NotTheAuthor,
            cancellationToken);

    /// <summary>Para eliminar: el autor, o un Admin u Owner del equipo moderando.</summary>
    public static Task<Result<(Comment Comment, Guid UserId, TeamRole Role)>> RequireDeletableAsync(
        ITeamAccess teamAccess,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        Guid commentId,
        CancellationToken cancellationToken) =>
        ResolveCommentAsync(
            teamAccess,
            currentUser,
            dbContext,
            teamKey,
            identifier,
            commentId,
            CommentPermissions.CanDelete,
            CommentErrors.CannotModerate,
            cancellationToken);

    private static async Task<Result<(Comment Comment, Guid UserId, TeamRole Role)>> ResolveCommentAsync(
        ITeamAccess teamAccess,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        Guid commentId,
        Func<Comment, Guid, TeamRole, bool> isAllowed,
        Error forbidden,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var context = await RequireIssueAsync(
            teamAccess, currentUser, dbContext, teamKey, identifier, cancellationToken);

        if (context.IsFailure)
        {
            return Result.Failure<(Comment, Guid, TeamRole)>(context.Error);
        }

        var (issue, userId, role) = context.Value;

        // Se busca rastreado: quien llega hasta acá viene a modificar el comentario.
        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(
                candidate => candidate.Id == commentId && candidate.IssueId == issue.Id,
                cancellationToken);

        // Un comentario ya eliminado se trata como inexistente, igual que en el listado:
        // que la respuesta cambie según si alguien lo borró o nunca existió no le sirve a
        // nadie más que a quien quiera sondear la conversación.
        if (comment is null || comment.IsDeleted)
        {
            return Result.Failure<(Comment, Guid, TeamRole)>(CommentErrors.NotFound);
        }

        return isAllowed(comment, userId, role)
            ? Result.Success((comment, userId, role))
            : Result.Failure<(Comment, Guid, TeamRole)>(forbidden);
    }
}
