namespace Linear.Domain.Activities;

/// <summary>
/// Algo que pasó dentro de un agregado y merece quedar registrado.
/// </summary>
/// <remarks>
/// Lo levanta el propio agregado, en el método donde ocurrió: es ahí —y solo ahí— donde se
/// sabe si cambiar el estado de un issue fue completarlo, cancelarlo o simplemente moverlo.
/// Mirar la fila antes y después no alcanzaría para distinguirlo, y menos para saber que
/// "asignar" es otra cosa que "editar".
///
/// El evento no es todavía una <see cref="Activity"/>: le faltan el actor —que el dominio no
/// conoce— y a veces el equipo. Los completa la infraestructura al guardar.
/// </remarks>
public sealed record ActivityEvent
{
    /// <summary>Payload vacío, para las acciones que no necesitan detalle.</summary>
    public static readonly IReadOnlyDictionary<string, string?> NoPayload =
        new Dictionary<string, string?>();

    public required ActivityEntityType EntityType { get; init; }

    /// <summary>Identificador de la entidad sobre la que ocurrió la acción.</summary>
    public required Guid EntityId { get; init; }

    public required ActivityAction Action { get; init; }

    /// <summary>
    /// Equipo al que pertenece lo que pasó, cuando el agregado lo sabe. Un comentario, por
    /// ejemplo, no lo sabe: solo conoce su issue.
    /// </summary>
    public Guid? TeamId { get; init; }

    /// <summary>
    /// Issue con el que se relaciona la acción, si hay alguno. Sirve para dos cosas: resolver
    /// el equipo cuando el agregado no lo conoce, y armar el historial de un issue incluyendo
    /// lo que pasó en sus comentarios.
    /// </summary>
    public Guid? IssueId { get; init; }

    /// <summary>
    /// Detalle de la acción, tal como lo plantea el modelo de dominio: por ejemplo
    /// <c>{ "oldValue": "Todo", "newValue": "InProgress" }</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Payload { get; init; } = NoPayload;
}

/// <summary>
/// Un agregado que registra lo que le va pasando para que se persista al guardar.
/// </summary>
/// <remarks>
/// Los eventos se acumulan en memoria y los drena la infraestructura dentro del mismo
/// <c>SaveChanges</c>: o se guardan el cambio y su registro juntos, o no se guarda ninguno.
/// </remarks>
public interface IHasActivity
{
    IReadOnlyList<ActivityEvent> PendingActivity { get; }

    void ClearActivity();
}
