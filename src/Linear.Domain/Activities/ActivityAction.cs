namespace Linear.Domain.Activities;

/// <summary>
/// Qué pasó. Es el vocabulario cerrado de la task 011.
/// </summary>
/// <remarks>
/// Se guarda como texto, así que agregar acciones nuevas no reinterpreta las viejas. Una
/// acción que la interfaz no sepa dibujar se muestra igual, con su nombre crudo: el historial
/// es append-only y no se puede reescribir para acomodarlo a una versión nueva del código.
/// </remarks>
public enum ActivityAction
{
    IssueCreated = 0,
    IssueUpdated = 1,
    IssueAssigned = 2,
    IssueCompleted = 3,
    IssueCanceled = 4,

    CommentCreated = 5,
    CommentUpdated = 6,

    LabelAdded = 7,
    LabelRemoved = 8,

    SprintStarted = 9,
    SprintCompleted = 10,

    RoadmapItemCreated = 11,
    RoadmapItemUpdated = 12
}
