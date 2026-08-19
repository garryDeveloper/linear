using Linear.Domain.Common;

namespace Linear.Domain.Comments;

public static class CommentErrors
{
    public static readonly Error ContentRequired =
        Error.Validation("Comments.ContentRequired", "El comentario no puede estar vacío.");

    public static readonly Error ContentTooLong = Error.Validation(
        "Comments.ContentTooLong",
        $"El comentario no puede superar los {Comment.MaxContentLength} caracteres.");

    public static readonly Error NotFound =
        Error.NotFound("Comments.NotFound", "No existe el comentario.");

    public static readonly Error AlreadyDeleted =
        Error.Conflict("Comments.AlreadyDeleted", "El comentario ya fue eliminado.");

    public static readonly Error Deleted =
        Error.Conflict("Comments.Deleted", "El comentario fue eliminado y ya no se puede editar.");

    /// <summary>
    /// Editar es privativo del autor: reescribir las palabras de otro no es moderar, es
    /// falsificar. Un Admin que necesita intervenir un comentario ajeno lo elimina.
    /// </summary>
    public static readonly Error NotTheAuthor =
        Error.Forbidden("Comments.NotTheAuthor", "Solo el autor puede editar su comentario.");

    public static readonly Error CannotModerate = Error.Forbidden(
        "Comments.CannotModerate",
        "Solo el autor o un Admin del equipo pueden eliminar el comentario.");
}
