using System.Collections.Concurrent;

using Linear.Web.Shared.Realtime;

using Microsoft.AspNetCore.SignalR;

namespace Linear.Web.Infrastructure.Realtime;

/// <summary>
/// Implementación única de <see cref="ITeamNotifier"/>.
/// </summary>
/// <remarks>
/// Es singleton porque el registro de suscriptores tiene que sobrevivir a los circuitos: un
/// aviso nace en la operación de un usuario y tiene que llegar a las pantallas de los otros,
/// que están en ámbitos distintos.
/// </remarks>
public sealed class TeamNotifier(
    IHubContext<TeamHub, ITeamClient> hub,
    ILogger<TeamNotifier> logger) : ITeamNotifier
{
    /// <summary>
    /// Suscriptores en proceso, por equipo.
    /// </summary>
    /// <remarks>
    /// El diccionario interno está indexado por un identificador de suscripción y no por el
    /// componente: dos pestañas del mismo usuario mirando la misma pantalla son dos
    /// suscripciones distintas, y una no debe dar de baja a la otra.
    /// </remarks>
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Func<TeamNotification, Task>>> _subscribers = new();

    public async Task PublishAsync(
        IReadOnlyCollection<TeamNotification> notifications,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        foreach (var notification in notifications)
        {
            await SendToHubAsync(notification, cancellationToken);
            await SendInProcessAsync(notification);
        }
    }

    public IDisposable Subscribe(Guid teamId, Func<TeamNotification, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.CreateVersion7();

        _subscribers.GetOrAdd(teamId, _ => new ConcurrentDictionary<Guid, Func<TeamNotification, Task>>())[id] = handler;

        return new Subscription(this, teamId, id);
    }

    private async Task SendToHubAsync(TeamNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients
                .Group(TeamHub.GroupFor(notification.TeamId))
                .ReceiveAsync(notification)
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // El cambio ya está guardado: que no se pueda avisar no lo deshace. Se registra
            // y se sigue, porque lo peor que pasa es que un cliente vea el dato viejo hasta
            // que vuelva a cargar.
            LogHubFailed(logger, notification.Event, notification.TeamId, exception);
        }
    }

    private async Task SendInProcessAsync(TeamNotification notification)
    {
        if (!_subscribers.TryGetValue(notification.TeamId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            try
            {
                await handler(notification);
            }
            catch (Exception exception)
            {
                // Un suscriptor que falla no puede dejar sin aviso a los demás. Pasa, por
                // ejemplo, cuando el circuito de una pestaña se cerró entre que se emitió
                // el aviso y que se entregó.
                LogSubscriberFailed(logger, notification.Event, notification.TeamId, exception);
            }
        }
    }

    private void Unsubscribe(Guid teamId, Guid subscriptionId)
    {
        if (!_subscribers.TryGetValue(teamId, out var handlers))
        {
            return;
        }

        handlers.TryRemove(subscriptionId, out _);

        // Un equipo sin nadie mirando no tiene por qué dejar su entrada ocupada. La carrera
        // con un alta simultánea se resuelve del lado seguro: si quedó alguien, no se quita.
        if (handlers.IsEmpty)
        {
            _subscribers.TryRemove(new KeyValuePair<Guid, ConcurrentDictionary<Guid, Func<TeamNotification, Task>>>(teamId, handlers));
        }
    }

    private sealed class Subscription(TeamNotifier notifier, Guid teamId, Guid id) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            notifier.Unsubscribe(teamId, id);
        }
    }

    private static readonly Action<ILogger, RealtimeEvent, Guid, Exception?> LogHubFailed =
        LoggerMessage.Define<RealtimeEvent, Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(LogHubFailed)),
            "No se pudo emitir {Event} al hub del equipo {TeamId}.");

    private static readonly Action<ILogger, RealtimeEvent, Guid, Exception?> LogSubscriberFailed =
        LoggerMessage.Define<RealtimeEvent, Guid>(
            LogLevel.Warning,
            new EventId(2, nameof(LogSubscriberFailed)),
            "Un suscriptor falló al recibir {Event} del equipo {TeamId}.");
}
