using System.Net.Http.Json;

using Linear.Domain.Issues;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Realtime;
using Linear.Web.Shared.Realtime;

namespace Linear.IntegrationTests.Features.Realtime;

/// <summary>
/// Que operar sobre la aplicación produzca los avisos que la task 014 enumera.
/// </summary>
/// <remarks>
/// Se opera por el API real y se escucha por el mismo camino que usan las pantallas Blazor:
/// una suscripción en proceso sobre <see cref="ITeamNotifier"/>. Eso ejercita la cadena
/// completa —handler, interceptor, transacción confirmada, reparto— sin depender de un
/// cliente de SignalR.
/// <para>
/// Lo que no se cubre acá es el salto por la red hasta un cliente externo: probar eso pedía
/// incorporar el paquete cliente de SignalR, que ninguna otra parte del proyecto usa. El
/// tramo que sí es propio —quién entra al grupo y qué se emite a cuál— está cubierto por
/// <see cref="TeamHubAuthorizationTests"/> y por los tests del repartidor.
/// </para>
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class RealtimeDeliveryTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public RealtimeDeliveryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _factory = new DatabaseWebApplicationFactory(postgres.ConnectionString);
    }

    public Task InitializeAsync() => _postgres.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatingAnIssueIsAnnounced()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        using var listener = Listen(team.Id);

        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var created = Assert.Single(listener.OfKind(RealtimeEvent.IssueCreated));

        Assert.Equal(issue.Id, created.EntityId);
        Assert.Equal(issue.Identifier, created.Identifier);
        Assert.Equal(team.Id, created.TeamId);
    }

    /// <summary>
    /// Cambiar el estado, la prioridad, el responsable o las labels llega como el mismo
    /// aviso: el cliente vuelve a pedir el issue y ve todo junto.
    /// </summary>
    [Theory]
    [InlineData("status")]
    [InlineData("priority")]
    public async Task ChangingAnIssueIsAnnounced(string what)
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var listener = Listen(team.Id);

        using var response = what == "status"
            ? await client.PutAsJsonAsync(
                $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/status",
                new { status = nameof(IssueStatus.InProgress) })
            : await client.PutAsJsonAsync(
                $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/priority",
                new { priority = nameof(IssuePriority.High) });

        response.EnsureSuccessStatusCode();

        var updated = Assert.Single(listener.OfKind(RealtimeEvent.IssueUpdated));

        Assert.Equal(issue.Identifier, updated.Identifier);
    }

    [Fact]
    public async Task AddingALabelIsAnnouncedAsAChangeToTheIssue()
    {
        var (team, _) = await ATeamAsync();
        var label = await TeamScenario.CreateLabelAsync(_factory, team.Id);
        using var client = await SignInAsync();
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var listener = Listen(team.Id);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels",
            new { labelId = label.Id });

        response.EnsureSuccessStatusCode();

        Assert.Single(listener.OfKind(RealtimeEvent.IssueUpdated));
    }

    /// <summary>
    /// Eliminar no deja actividad —el historial es append-only y la task 011 no definió esa
    /// acción—, así que este aviso no puede derivarse del historial: sale del propio guardado.
    /// </summary>
    [Fact]
    public async Task DeletingAnIssueIsAnnounced()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var listener = Listen(team.Id);

        using var response = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}");

        response.EnsureSuccessStatusCode();

        var deleted = Assert.Single(listener.OfKind(RealtimeEvent.IssueDeleted));

        Assert.Equal(issue.Identifier, deleted.Identifier);
    }

    [Fact]
    public async Task CommentingIsAnnouncedWithTheIssueItBelongsTo()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var listener = Listen(team.Id);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/comments",
            new { content = "Lo miro yo." });

        response.EnsureSuccessStatusCode();

        var created = Assert.Single(listener.OfKind(RealtimeEvent.CommentCreated));

        // El identificador viaja para que la pantalla del hilo pueda descartar lo ajeno sin
        // consultar la base.
        Assert.Equal(issue.Identifier, created.Identifier);
        Assert.Equal(issue.Id, created.IssueId);
    }

    /// <summary>Borrar un comentario es marcarlo, no borrar la fila: hay que distinguirlo.</summary>
    [Fact]
    public async Task DeletingACommentIsAnnouncedAsADeletionAndNotAsAnEdit()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var created = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/comments",
            new { content = "Lo miro yo." });

        created.EnsureSuccessStatusCode();

        var comment = await created.Content.ReadFromJsonAsync<Linear.Web.Features.Comments.Contracts.CommentResponse>();

        using var listener = Listen(team.Id);

        using var response = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/comments/{comment!.Id}");

        response.EnsureSuccessStatusCode();

        Assert.Single(listener.OfKind(RealtimeEvent.CommentDeleted));
        Assert.Empty(listener.OfKind(RealtimeEvent.CommentUpdated));
    }

    [Fact]
    public async Task StartingASprintIsAnnounced()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();

        using var created = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/sprints",
            new
            {
                name = "Sprint 1",
                startDate = DateOnly.FromDateTime(DateTime.UtcNow),
                endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
            });

        created.EnsureSuccessStatusCode();

        var sprint = await created.Content.ReadFromJsonAsync<SprintResponse>();

        using var listener = Listen(team.Id);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/sprints/{sprint!.Id}/start",
            new { });

        response.EnsureSuccessStatusCode();

        var updated = Assert.Single(listener.OfKind(RealtimeEvent.SprintUpdated));

        Assert.Equal(sprint.Id, updated.EntityId);
    }

    /// <summary>
    /// El feed de actividad se actualiza solo. Este test cubre además el orden de los dos
    /// interceptores: si el de tiempo real corriera antes que el de actividad, las filas de
    /// <c>Activity</c> todavía no existirían y este aviso no se emitiría nunca.
    /// </summary>
    [Fact]
    public async Task ActivityIsAnnounced()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        using var listener = Listen(team.Id);

        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var activity = Assert.Single(listener.OfKind(RealtimeEvent.ActivityCreated));

        Assert.Equal(issue.Identifier, activity.Identifier);
        Assert.Equal(team.Id, activity.TeamId);
    }

    /// <summary>
    /// Aislamiento por equipo de punta a punta: lo que pasa en un equipo no llega a quien
    /// escucha otro.
    /// </summary>
    [Fact]
    public async Task NothingLeaksToAnotherTeam()
    {
        var (team, owner) = await ATeamAsync();
        var other = await TeamScenario.CreateTeamAsync(_factory, "API", owner, "API");
        using var client = await SignInAsync();

        using var listener = Listen(other.Id);

        await CreateIssueAsync(client, team, "Arreglar el login");

        Assert.Empty(listener.Received);
    }

    /// <summary>
    /// Un aviso no se puede deshacer, así que se emite recién cuando la transacción confirmó.
    /// Acá la operación falla en validación y no debe anunciarse nada.
    /// </summary>
    [Fact]
    public async Task NothingIsAnnouncedWhenTheOperationFails()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync();
        using var listener = Listen(team.Id);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = string.Empty });

        Assert.False(response.IsSuccessStatusCode);
        Assert.Empty(listener.Received);
    }

    /// <summary>
    /// Cada aviso dice quién lo provocó, que es lo que permite que el autor no reciba su
    /// propio eco y no se le mueva la pantalla.
    /// </summary>
    [Fact]
    public async Task EveryNotificationSaysWhoCausedIt()
    {
        var (team, owner) = await ATeamAsync();
        using var client = await SignInAsync();
        using var listener = Listen(team.Id);

        await CreateIssueAsync(client, team, "Arreglar el login");

        Assert.NotEmpty(listener.Received);
        Assert.All(listener.Received, notification => Assert.Equal(owner, notification.ActorUserId));
    }

    // ---- andamiaje ----------------------------------------------------------------------

    /// <summary>Se engancha al repartidor real de la aplicación y guarda lo que llega.</summary>
    private Listener Listen(Guid teamId)
    {
        var notifier = _factory.Services.GetRequiredService<ITeamNotifier>();

        return new Listener(notifier, teamId);
    }

    private sealed class Listener : IDisposable
    {
        private readonly List<TeamNotification> _received = [];
        private readonly IDisposable _subscription;
        private readonly Lock _gate = new();

        public Listener(ITeamNotifier notifier, Guid teamId) =>
            _subscription = notifier.Subscribe(teamId, notification =>
            {
                lock (_gate)
                {
                    _received.Add(notification);
                }

                return Task.CompletedTask;
            });

        public IReadOnlyList<TeamNotification> Received
        {
            get
            {
                lock (_gate)
                {
                    return [.. _received];
                }
            }
        }

        public IReadOnlyList<TeamNotification> OfKind(RealtimeEvent kind) =>
            [.. Received.Where(notification => notification.Event == kind)];

        public void Dispose() => _subscription.Dispose();
    }

    private async Task<(Team Team, Guid Owner)> ATeamAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        return (team, owner.Id);
    }

    private Task<HttpClient> SignInAsync() =>
        AuthenticationScenario.SignInAsync(_factory, OwnerEmail, AuthenticationScenario.DefaultPassword);

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Team team, string title)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }
}
