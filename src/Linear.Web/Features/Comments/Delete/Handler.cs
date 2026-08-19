using Linear.Domain.Common;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Comments.Delete;

/// <summary>
/// Elimina un comentario propio, o uno ajeno si quien lo pide modera el equipo.
/// </summary>
/// <remarks>
/// La eliminación es lógica: la fila queda con <c>DeletedAt</c> y desaparece del listado.
/// A diferencia de eliminar un issue, no hace falta rol Admin para el caso normal —cada uno
/// puede retirar lo que escribió—; el rol solo entra en juego para moderar lo ajeno.
/// </remarks>
public sealed class DeleteCommentHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(DeleteCommentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var context = await CommentAccess.RequireDeletableAsync(
            teamAccess,
            currentUser,
            dbContext,
            request.Key,
            request.Identifier,
            request.CommentId,
            cancellationToken);

        if (context.IsFailure)
        {
            return Result.Failure(context.Error);
        }

        var deleted = context.Value.Comment.Delete(DateTimeOffset.UtcNow);

        if (deleted.IsFailure)
        {
            return deleted;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
