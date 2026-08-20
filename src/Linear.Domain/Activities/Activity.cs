namespace Linear.Domain.Activities;

/// <summary>
/// Registro histórico de algo que pasó en un equipo.
/// </summary>
/// <remarks>
/// Es append-only, como pide la task 011: se crea y nunca más se toca. Por eso no expone un
/// solo método que la modifique —ni siquiera internos— y la configuración de EF le quita el
/// seguimiento de cambios. No hay operación de editar ni de eliminar en toda la aplicación.
///
/// Que sea inmutable es también lo que la vuelve confiable: un historial que se puede
/// reescribir no sirve para auditar nada.
/// </remarks>
public sealed class Activity
{
    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Activity()
    {
    }

    private Activity(
        Guid teamId,
        Guid userId,
        ActivityEntityType entityType,
        Guid entityId,
        ActivityAction action,
        string payloadJson,
        DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        TeamId = teamId;
        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        PayloadJson = payloadJson;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Equipo dueño del historial. El feed del equipo filtra por acá.</summary>
    public Guid TeamId { get; private set; }

    /// <summary>Quién lo hizo.</summary>
    public Guid UserId { get; private set; }

    public ActivityEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public ActivityAction Action { get; private set; }

    /// <summary>
    /// Detalle de la acción en JSON. Se guarda como texto y no como columnas porque cada
    /// acción tiene su propia forma: un cambio de estado lleva valor viejo y nuevo, agregar
    /// una label lleva su nombre, y forzarlas a un esquema común llenaría la tabla de nulos.
    /// </summary>
    public string PayloadJson { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Crea el registro. Es el único camino: no hay forma de construir una actividad que no
    /// sea registrándola.
    /// </summary>
    public static Activity Record(
        Guid teamId,
        Guid userId,
        ActivityEntityType entityType,
        Guid entityId,
        ActivityAction action,
        string payloadJson,
        DateTimeOffset now) =>
        new(teamId, userId, entityType, entityId, action, payloadJson, now);
}
