using Linear.Domain.Comments;
using Linear.Domain.Teams;

namespace Linear.Web.Features.Comments.Contracts;

/// <summary>
/// Las reglas de quién puede editar y quién puede eliminar un comentario.
/// </summary>
/// <remarks>
/// Viven en un único lugar y no repartidas entre los handlers y la interfaz: los handlers
/// las aplican para autorizar, y <see cref="CommentResponseMapper"/> las evalúa para que
/// cada comentario viaje diciendo qué puede hacer con él quien lo está mirando. Si la
/// pantalla dedujera los permisos por su cuenta, tarde o temprano mostraría un botón que
/// el servidor rechaza —o escondería uno que sí estaba permitido—.
///
/// Editar y eliminar no son el mismo permiso a propósito. Un Admin modera eliminando; el
/// texto de un comentario solo lo cambia quien lo escribió.
/// </remarks>
public static class CommentPermissions
{
    public static bool CanEdit(Comment comment, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(comment);

        return comment.IsAuthoredBy(userId);
    }

    public static bool CanDelete(Comment comment, Guid userId, TeamRole role)
    {
        ArgumentNullException.ThrowIfNull(comment);

        return comment.IsAuthoredBy(userId) || role >= TeamRole.Admin;
    }
}
