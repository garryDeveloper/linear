namespace Linear.Web.Shared.Realtime;

/// <summary>
/// Aviso de que algo cambió en un equipo.
/// </summary>
/// <remarks>
/// Lleva lo justo para que quien lo recibe decida si le interesa y vuelva a pedir el dato:
/// qué pasó, en qué equipo, y sobre qué entidad. No lleva el issue ni el comentario en sí.
/// <para>
/// Es a propósito. Un payload con el estado completo obligaría a resolver acá los permisos
/// de cada destinatario —quién puede ver qué— y a mantener el mapeo a DTO en dos lugares.
/// Avisar y dejar que el cliente pida por el camino de siempre reusa el handler que ya
/// aplica esas reglas. A cambio hay una consulta más, que solo pagan las pantallas que
/// justo están mirando lo que cambió.
/// </para>
/// </remarks>
public sealed record TeamNotification
{
    public required RealtimeEvent Event { get; init; }

    /// <summary>Equipo dueño del cambio. Es lo que aísla a un equipo de otro.</summary>
    public required Guid TeamId { get; init; }

    /// <summary>Issue con el que se relaciona el cambio, si hay alguno.</summary>
    public Guid? IssueId { get; init; }

    /// <summary>
    /// Identificador legible del issue —<c>WEB-12</c>—, cuando se conoce.
    /// </summary>
    /// <remarks>
    /// Viaja además del <see cref="IssueId"/> porque las pantallas se enrutan por él: sin
    /// esto, una vista de detalle tendría que consultar la base solo para saber si el aviso
    /// era sobre el issue que está mostrando.
    /// </remarks>
    public string? Identifier { get; init; }

    /// <summary>
    /// Entidad concreta del evento: el comentario, el sprint o la actividad. Para los
    /// eventos de issue coincide con <see cref="IssueId"/>.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Quién lo provocó, si había sesión.
    /// </summary>
    /// <remarks>
    /// Permite que quien hizo el cambio ignore su propio eco: su pantalla ya se actualizó
    /// con la respuesta de la operación, y volver a cargar le movería el scroll sin motivo.
    /// Es nulo cuando el cambio no viene de una sesión, como en la siembra de datos.
    /// </remarks>
    public Guid? ActorUserId { get; init; }
}
