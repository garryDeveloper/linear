using Linear.Web.Infrastructure.Realtime;
using Linear.Web.Shared.Realtime;

using Microsoft.Extensions.Logging.Abstractions;

namespace Linear.UnitTests.Realtime;

/// <summary>
/// El repartidor de avisos.
/// </summary>
/// <remarks>
/// Lo que se fija acá es el aislamiento por equipo —criterio de aceptación de la task 014— y
/// que ninguno de los dos destinos pueda arruinar al otro.
/// </remarks>
public class TeamNotifierTests
{
    private static TeamNotifier Create(FakeHubContext hub) =>
        new(hub, NullLogger<TeamNotifier>.Instance);

    private static TeamNotification Notification(Guid teamId, RealtimeEvent kind = RealtimeEvent.IssueUpdated) =>
        new()
        {
            Event = kind,
            TeamId = teamId,
            EntityId = Guid.CreateVersion7()
        };

    [Fact]
    public async Task PublishesToTheGroupOfItsTeam()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        var sent = Assert.Single(hub.SentTo(TeamHub.GroupFor(teamId)));

        Assert.Equal(RealtimeEvent.IssueUpdated, sent.Event);
        Assert.Equal(teamId, sent.TeamId);
    }

    /// <summary>Dos equipos, dos grupos: un aviso no se cruza de uno a otro.</summary>
    [Fact]
    public async Task DoesNotMixUpTeams()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var mine = Guid.CreateVersion7();
        var theirs = Guid.CreateVersion7();

        await notifier.PublishAsync([Notification(mine)], CancellationToken.None);

        Assert.Single(hub.SentTo(TeamHub.GroupFor(mine)));
        Assert.Empty(hub.SentTo(TeamHub.GroupFor(theirs)));
    }

    [Fact]
    public async Task DeliversToSubscribersInProcess()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();
        var received = new List<TeamNotification>();

        using var subscription = notifier.Subscribe(teamId, notification =>
        {
            received.Add(notification);
            return Task.CompletedTask;
        });

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        Assert.Single(received);
    }

    /// <summary>
    /// El aislamiento vale también para el camino en proceso, que no pasa por el hub. Si solo
    /// se comprobara del lado de SignalR, una pantalla Blazor podría ver lo de otro equipo.
    /// </summary>
    [Fact]
    public async Task SubscribersOnlyHearAboutTheirOwnTeam()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var mine = Guid.CreateVersion7();
        var theirs = Guid.CreateVersion7();
        var received = new List<TeamNotification>();

        using var subscription = notifier.Subscribe(mine, notification =>
        {
            received.Add(notification);
            return Task.CompletedTask;
        });

        await notifier.PublishAsync([Notification(theirs)], CancellationToken.None);

        Assert.Empty(received);
    }

    [Fact]
    public async Task StopsDeliveringAfterUnsubscribing()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();
        var received = 0;

        var subscription = notifier.Subscribe(teamId, _ =>
        {
            received++;
            return Task.CompletedTask;
        });

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        subscription.Dispose();

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        Assert.Equal(1, received);
    }

    /// <summary>
    /// Dos pestañas del mismo usuario en la misma pantalla son dos suscripciones: cerrar una
    /// no puede dejar sorda a la otra.
    /// </summary>
    [Fact]
    public async Task UnsubscribingOneLeavesTheOthers()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();
        var first = 0;
        var second = 0;

        var one = notifier.Subscribe(teamId, _ => { first++; return Task.CompletedTask; });
        using var two = notifier.Subscribe(teamId, _ => { second++; return Task.CompletedTask; });

        one.Dispose();

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
    }

    /// <summary>
    /// Un suscriptor que explota —una pestaña que se cerró en el medio— no puede dejar sin
    /// aviso a los demás.
    /// </summary>
    [Fact]
    public async Task OneBrokenSubscriberDoesNotStopTheRest()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();
        var healthy = 0;

        using var broken = notifier.Subscribe(
            teamId,
            _ => throw new InvalidOperationException("El circuito ya no existe."));

        using var ok = notifier.Subscribe(teamId, _ => { healthy++; return Task.CompletedTask; });

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        Assert.Equal(1, healthy);
    }

    /// <summary>
    /// El cambio ya está guardado cuando se emite: que el hub falle no puede tumbar la
    /// operación ni impedir que se entregue por el otro camino.
    /// </summary>
    [Fact]
    public async Task AFailingHubDoesNotBreakTheOperation()
    {
        var hub = new FakeHubContext();
        var notifier = Create(hub);
        var teamId = Guid.CreateVersion7();
        var received = 0;

        hub.FailNextSend();

        using var subscription = notifier.Subscribe(teamId, _ => { received++; return Task.CompletedTask; });

        await notifier.PublishAsync([Notification(teamId)], CancellationToken.None);

        Assert.Equal(1, received);
    }

    /// <summary>El nombre del grupo es lo que separa un equipo de otro.</summary>
    [Fact]
    public void EachTeamHasItsOwnGroup()
    {
        var one = Guid.CreateVersion7();
        var another = Guid.CreateVersion7();

        Assert.NotEqual(TeamHub.GroupFor(one), TeamHub.GroupFor(another));
        Assert.Equal(TeamHub.GroupFor(one), TeamHub.GroupFor(one));
    }
}
