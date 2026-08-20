using System.Collections.Concurrent;

using Linear.Web.Infrastructure.Realtime;
using Linear.Web.Shared.Realtime;

using Microsoft.AspNetCore.SignalR;

namespace Linear.UnitTests.Realtime;

/// <summary>
/// Hub de mentira que anota a qué grupo se emitió cada aviso.
/// </summary>
/// <remarks>
/// Escrito a mano y no con una librería de dobles: el proyecto no incorpora dependencias
/// nuevas, y de <c>IHubContext</c> hace falta una sola cosa —qué se mandó y a qué grupo—.
/// </remarks>
internal sealed class FakeHubContext : IHubContext<TeamHub, ITeamClient>
{
    private readonly HubClients _clients = new();

    public IHubClients<ITeamClient> Clients => _clients;

    public IGroupManager Groups { get; } = new NotUsedGroupManager();

    /// <summary>Lo emitido, por nombre de grupo y en orden.</summary>
    public IReadOnlyList<TeamNotification> SentTo(string group) =>
        _clients.Sent.TryGetValue(group, out var sent) ? [.. sent] : [];

    /// <summary>Hace fallar la próxima emisión, para probar que un fallo no se propaga.</summary>
    public void FailNextSend() => _clients.FailNext = true;

    private sealed class HubClients : IHubClients<ITeamClient>
    {
        public ConcurrentDictionary<string, List<TeamNotification>> Sent { get; } = new();

        public bool FailNext { get; set; }

        public ITeamClient Group(string groupName) => new Recorder(this, groupName);

        public ITeamClient All => throw new NotSupportedException();

        public ITeamClient AllExcept(IReadOnlyList<string> excludedConnectionIds) =>
            throw new NotSupportedException();

        public ITeamClient Client(string connectionId) => throw new NotSupportedException();

        public ITeamClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public ITeamClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) =>
            throw new NotSupportedException();

        public ITeamClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();

        public ITeamClient User(string userId) => throw new NotSupportedException();

        public ITeamClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class Recorder(HubClients clients, string group) : ITeamClient
    {
        public Task ReceiveAsync(TeamNotification notification)
        {
            if (clients.FailNext)
            {
                clients.FailNext = false;

                return Task.FromException(new InvalidOperationException("Falla simulada del hub."));
            }

            clients.Sent.GetOrAdd(group, _ => []).Add(notification);

            return Task.CompletedTask;
        }
    }

    private sealed class NotUsedGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
