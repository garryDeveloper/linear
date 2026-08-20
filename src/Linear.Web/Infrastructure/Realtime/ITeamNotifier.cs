using Linear.Web.Shared.Realtime;

namespace Linear.Web.Infrastructure.Realtime;

/// <summary>
/// Reparte los avisos de cambios de un equipo.
/// </summary>
/// <remarks>
/// Tiene dos destinos y por eso una sola pieza los coordina.
/// <list type="bullet">
/// <item>
/// El <see cref="TeamHub"/>, para clientes que están del otro lado de la red.
/// </item>
/// <item>
/// Suscriptores en proceso: los componentes Blazor Server, que ya corren en el servidor y
/// tienen su propio canal con el navegador —el circuito—. Hacerlos abrir una conexión de
/// SignalR contra su propia aplicación sumaría un websocket y una ronda de autenticación
/// por usuario para entregar un mensaje que nace a metros de distancia.
/// </item>
/// </list>
/// Los dos destinos reciben exactamente el mismo <see cref="TeamNotification"/>: el contrato
/// es uno solo, lo que cambia es el transporte.
/// </remarks>
public interface ITeamNotifier
{
    /// <summary>Emite los avisos a todos los destinos.</summary>
    Task PublishAsync(IReadOnlyCollection<TeamNotification> notifications, CancellationToken cancellationToken);

    /// <summary>
    /// Registra un interesado en los cambios de un equipo.
    /// </summary>
    /// <returns>
    /// La baja de la suscripción. Descartarla es obligatorio: sin eso, el componente queda
    /// referenciado desde un servicio singleton y no lo recoge el recolector.
    /// </returns>
    IDisposable Subscribe(Guid teamId, Func<TeamNotification, Task> handler);
}
