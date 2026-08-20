namespace Linear.Web.Shared.Realtime;

/// <summary>
/// Lo que el servidor le puede mandar a un cliente conectado al hub del equipo.
/// </summary>
/// <remarks>
/// Es una interfaz y no una llamada por nombre —<c>SendAsync("Receive", ...)</c>— para que
/// el contrato lo verifique el compilador: renombrar el evento o cambiarle la forma al
/// payload rompe la compilación en vez de dejar de entregar mensajes en silencio, que es
/// como fallan los hubs escritos con literales.
/// </remarks>
public interface ITeamClient
{
    /// <summary>Avisa que algo cambió en un equipo al que el cliente está suscripto.</summary>
    Task ReceiveAsync(TeamNotification notification);
}
