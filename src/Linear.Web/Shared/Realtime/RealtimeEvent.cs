namespace Linear.Web.Shared.Realtime;

/// <summary>
/// Qué cambió. Es el vocabulario cerrado de la task 014.
/// </summary>
/// <remarks>
/// Es deliberadamente más pobre que <c>ActivityAction</c>. El historial necesita saber que
/// cambiar el estado a <c>Done</c> fue "completar" y no "editar"; un cliente conectado no:
/// solo necesita saber que el issue cambió para volver a pedirlo. Distinguir acá entre
/// cambiar la prioridad y cambiar el responsable obligaría a mantener dos vocabularios en
/// paralelo, y a tocar este enum cada vez que el dominio gana un método.
/// <para>
/// El detalle de qué pasó ya viaja, y por el camino que corresponde: como una actividad.
/// </para>
/// </remarks>
public enum RealtimeEvent
{
    IssueCreated = 0,
    IssueUpdated = 1,
    IssueDeleted = 2,

    CommentCreated = 3,
    CommentUpdated = 4,
    CommentDeleted = 5,

    SprintUpdated = 6,

    ActivityCreated = 7
}
