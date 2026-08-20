using System.Security.Claims;

using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Infrastructure.Realtime;
using Linear.Web.Shared.Realtime;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Realtime;

/// <summary>
/// Quién puede suscribirse al hub de un equipo.
/// </summary>
/// <remarks>
/// Es el criterio de aceptación "tests de autorización del Hub" de la task 014, y lo que
/// sostiene el aislamiento entre equipos: los avisos se emiten a un grupo, así que todo el
/// control está en quién logra entrar al grupo.
/// <para>
/// El hub se invoca directamente, con un contexto de conexión armado a mano, en lugar de
/// levantar un cliente de SignalR. Así se ejercita la decisión de autorización real —contra
/// la base y contra <see cref="ITeamAccess"/>— sin sumar al proyecto una dependencia de
/// cliente que ninguna otra parte usa.
/// </para>
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class TeamHubAuthorizationTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public TeamHubAuthorizationTests(PostgresFixture postgres)
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
    public async Task AMemberOfTheTeamCanSubscribe()
    {
        var (team, owner) = await ATeamAsync();

        var (hub, groups) = CreateHub(owner);

        Assert.True(await hub.SubscribeAsync(team.Key.Value));
        Assert.Equal(TeamHub.GroupFor(team.Id), Assert.Single(groups.Added));
    }

    /// <summary>El rol más bajo alcanza: recibir avisos es leer.</summary>
    [Fact]
    public async Task APlainMemberCanSubscribeToo()
    {
        var (team, _) = await ATeamAsync(withMember: true);
        var member = await UserIdAsync(MemberEmail);

        var (hub, groups) = CreateHub(member);

        Assert.True(await hub.SubscribeAsync(team.Key.Value));
        Assert.Single(groups.Added);
    }

    /// <summary>
    /// Lo esencial: quien no pertenece al equipo no entra al grupo, y por lo tanto no recibe
    /// nada de ese equipo.
    /// </summary>
    [Fact]
    public async Task SomeoneOutsideTheTeamCannotSubscribe()
    {
        var (team, _) = await ATeamAsync();
        var outsider = await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        var (hub, groups) = CreateHub(outsider.Id);

        Assert.False(await hub.SubscribeAsync(team.Key.Value));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task AConnectionWithoutIdentityCannotSubscribe()
    {
        var (team, _) = await ATeamAsync();

        var (hub, groups) = CreateHub(userId: null);

        Assert.False(await hub.SubscribeAsync(team.Key.Value));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task AnUnknownTeamCannotBeSubscribedTo()
    {
        var (_, owner) = await ATeamAsync();

        var (hub, groups) = CreateHub(owner);

        Assert.False(await hub.SubscribeAsync("NOPE"));
        Assert.Empty(groups.Added);
    }

    /// <summary>
    /// Una clave con forma inválida se rechaza antes de tocar la base, y con la misma
    /// respuesta que una que no existe.
    /// </summary>
    [Fact]
    public async Task AMalformedKeyCannotBeSubscribedTo()
    {
        var (_, owner) = await ATeamAsync();

        var (hub, groups) = CreateHub(owner);

        Assert.False(await hub.SubscribeAsync("   "));
        Assert.Empty(groups.Added);
    }

    /// <summary>
    /// Darse de baja también comprueba la pertenencia: sin eso, este método le diría a
    /// cualquiera qué claves de equipo existen.
    /// </summary>
    [Fact]
    public async Task SomeoneOutsideTheTeamCannotUnsubscribeEither()
    {
        var (team, _) = await ATeamAsync();
        var outsider = await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        var (hub, groups) = CreateHub(outsider.Id);

        await hub.UnsubscribeAsync(team.Key.Value);

        Assert.Empty(groups.Removed);
    }

    [Fact]
    public async Task AMemberCanUnsubscribe()
    {
        var (team, owner) = await ATeamAsync();

        var (hub, groups) = CreateHub(owner);

        await hub.UnsubscribeAsync(team.Key.Value);

        Assert.Equal(TeamHub.GroupFor(team.Id), Assert.Single(groups.Removed));
    }

    /// <summary>El hub exige autenticación antes de que se pueda invocar nada.</summary>
    [Fact]
    public void TheHubRequiresAuthentication()
    {
        var authorize = typeof(TeamHub)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);

        Assert.NotEmpty(authorize);
    }

    // ---- andamiaje ----------------------------------------------------------------------

    private (TeamHub Hub, RecordingGroupManager Groups) CreateHub(Guid? userId)
    {
        var scope = _factory.Services.CreateScope();

        var hub = new TeamHub(
            scope.ServiceProvider.GetRequiredService<ITeamAccess>(),
            scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>());

        var groups = new RecordingGroupManager();

        hub.Context = new FakeHubCallerContext(userId);
        hub.Groups = groups;

        return (hub, groups);
    }

    private async Task<(Team Team, Guid Owner)> ATeamAsync(bool withMember = false)
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        if (withMember)
        {
            var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
            await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);
        }

        return (team, owner.Id);
    }

    private async Task<Guid> UserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var address = Email.Create(email).Value;
        var user = await dbContext.Users.FirstAsync(candidate => candidate.Email == address);

        return user.Id;
    }

    /// <summary>Anota a qué grupos se entró y de cuáles se salió.</summary>
    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<string> Added { get; } = [];

        public List<string> Removed { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add(groupName);
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Removed.Add(groupName);
            return Task.CompletedTask;
        }
    }

    /// <summary>Una conexión con —o sin— identidad, que es lo único que el hub consulta.</summary>
    private sealed class FakeHubCallerContext(Guid? userId) : HubCallerContext
    {
        public override string ConnectionId { get; } = Guid.CreateVersion7().ToString();

        public override string? UserIdentifier => userId?.ToString();

        public override ClaimsPrincipal? User { get; } = userId is { } id
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, id.ToString())],
                authenticationType: "Test"))
            : new ClaimsPrincipal(new ClaimsIdentity());

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
