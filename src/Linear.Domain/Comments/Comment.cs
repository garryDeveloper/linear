using Linear.Domain.Activities;
using Linear.Domain.Common;

namespace Linear.Domain.Comments;

/// <summary>
/// Comentario dentro de un issue. Es la unidad de conversación del sistema.
/// </summary>
/// <remarks>
/// El modelo de dominio (.ai/domain-model.md) ubica los comentarios dentro del agregado
/// <c>Issue</c>. Acá se modelan como raíz propia por la misma razón que <c>Label</c> no
/// vive dentro de <c>Team</c>: la colección crece sin techo —un issue discutido junta
/// decenas de comentarios— y cargarla entera cada vez que se abre el issue chocaría con
/// las dos reglas de rendimiento del proyecto, "paginación obligatoria en listados" y
/// "evitar Include innecesarios" (.ai/architecture.md). El comentario referencia al issue
/// y a su autor solo por identificador, nunca como objeto.
///
/// El contenido se guarda tal como lo escribió el usuario, en Markdown y sin interpretar:
/// el renderizado y la sanitización son de la task 012. Guardar el texto crudo es
/// justamente lo que deja implementarla después sin migrar datos.
/// </remarks>
public sealed class Comment : IHasActivity
{
    /// <summary>
    /// A diferencia de <c>Issue.Description</c>, que no tiene tope, un comentario sí lo
    /// tiene: son muchos por issue y los escribe cualquier miembro, así que un límite
    /// generoso acota el abuso sin estorbar a nadie que esté comentando de buena fe.
    /// </summary>
    public const int MaxContentLength = 10_000;

    private readonly List<ActivityEvent> _activity = [];

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Comment()
    {
    }

    private Comment(Guid id, Guid issueId, Guid authorId, string content, DateTimeOffset now)
    {
        Id = id;
        IssueId = issueId;
        AuthorId = authorId;
        Content = content.Trim();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Issue al que pertenece. Un comentario no se mueve de issue.</summary>
    public Guid IssueId { get; private set; }

    public Guid AuthorId { get; private set; }

    /// <summary>Texto en Markdown, sin interpretar.</summary>
    public string Content { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Momento en que se eliminó el comentario, si se eliminó.
    /// </summary>
    /// <remarks>
    /// La eliminación es lógica y no física porque el modelo define el campo: el registro
    /// sobrevive para que la actividad de la task 011 —que es append-only y va a referenciar
    /// comentarios— no quede apuntando a una fila que ya no está. De cara al usuario el
    /// efecto es el mismo: un comentario eliminado no aparece en el listado.
    /// </remarks>
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>
    /// Indica si el comentario se editó después de publicado, para poder marcarlo en la
    /// interfaz. Eliminar no cuenta como editar: por eso <see cref="Delete"/> no toca
    /// <see cref="UpdatedAt"/>.
    /// </summary>
    public bool IsEdited => UpdatedAt > CreatedAt;

    public static Result<Comment> Create(Guid issueId, Guid authorId, string content, DateTimeOffset now)
    {
        var validation = ValidateContent(content);

        if (validation.IsFailure)
        {
            return Result.Failure<Comment>(validation.Error);
        }

        var comment = new Comment(Guid.CreateVersion7(), issueId, authorId, content, now);

        comment.Record(ActivityAction.CommentCreated);

        return Result.Success(comment);
    }

    public Result UpdateContent(string content, DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return Result.Failure(CommentErrors.Deleted);
        }

        var validation = ValidateContent(content);

        if (validation.IsFailure)
        {
            return validation;
        }

        Content = content.Trim();
        UpdatedAt = now;

        Record(ActivityAction.CommentUpdated);

        return Result.Success();
    }

    public Result Delete(DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return Result.Failure(CommentErrors.AlreadyDeleted);
        }

        DeletedAt = now;

        return Result.Success();
    }

    public bool IsAuthoredBy(Guid userId) => AuthorId == userId;

    /// <inheritdoc />
    public IReadOnlyList<ActivityEvent> PendingActivity => _activity.AsReadOnly();

    /// <inheritdoc />
    public void ClearActivity() => _activity.Clear();

    /// <summary>
    /// Registra la actividad del comentario.
    /// </summary>
    /// <remarks>
    /// Sin equipo: un comentario solo conoce su issue. Lo resuelve la infraestructura al
    /// guardar, siguiendo el <see cref="IssueId"/> — que además es lo que hace que el
    /// comentario aparezca en el historial del issue y no solo en el del equipo.
    ///
    /// Eliminar no registra actividad: la task 011 no lista una acción para eso, y como el
    /// historial es append-only, inventarla sería agregar vocabulario que nadie definió.
    /// </remarks>
    private void Record(ActivityAction action) =>
        _activity.Add(new ActivityEvent
        {
            EntityType = ActivityEntityType.Comment,
            EntityId = Id,
            Action = action,
            IssueId = IssueId
        });

    private static Result ValidateContent(string content) => content switch
    {
        _ when string.IsNullOrWhiteSpace(content) => Result.Failure(CommentErrors.ContentRequired),
        _ when content.Trim().Length > MaxContentLength => Result.Failure(CommentErrors.ContentTooLong),
        _ => Result.Success()
    };
}
