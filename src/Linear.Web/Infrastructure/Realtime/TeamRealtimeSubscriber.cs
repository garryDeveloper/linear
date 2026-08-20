using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Realtime;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Realtime;

/// <summary>
/// Puerta de entrada de los componentes Blazor al tiempo real.
/// </summary>
/// <remarks>
/// Un componente conoce la clave del equipo —la trae la ruta—, no su identificador, y no
/// tiene por qué saber que los avisos se agrupan por identificador. Además, y sobre todo,
/// suscribirse tiene que pasar por el mismo control de pertenencia que el hub: el camino en
/// proceso no atraviesa <see cref="TeamHub"/>, así que si la comprobación viviera solo allá,
/// esta puerta sería una forma de recibir los cambios de un equipo ajeno.
/// </remarks>
public sealed class TeamRealtimeSubscriber(
    ITeamNotifier notifier,
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    /// <summary>
    /// Suscribe al equipo indicado.
    /// </summary>
    /// <param name="includeOwnChanges">
    /// Si el propio autor del cambio debe recibir su eco. Por omisión no: su pantalla ya se
    /// actualizó con la respuesta de la operación, y volver a cargarla le movería el scroll
    /// —o le cerraría un menú abierto— sin que hubiera novedad para él.
    /// </param>
    /// <returns>
    /// La baja de la suscripción, o <c>null</c> si el usuario no pertenece al equipo o el
    /// equipo no existe. Se devuelve nulo en vez de un error porque para el componente son
    /// el mismo caso: no hay nada que escuchar.
    /// </returns>
    public async Task<IDisposable?> SubscribeAsync(
        string teamKey,
        Func<TeamNotification, Task> handler,
        CancellationToken cancellationToken,
        bool includeOwnChanges = false)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var key = TeamKey.Create(teamKey);

        if (key.IsFailure)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await teamAccess.RequireRoleAsync(
            dbContext, key.Value, TeamRole.Member, tracking: false, cancellationToken);

        if (team.IsFailure)
        {
            return null;
        }

        if (includeOwnChanges)
        {
            return notifier.Subscribe(team.Value.Id, handler);
        }

        var actor = await currentUser.RequireIdAsync(cancellationToken);

        if (actor.IsFailure)
        {
            return notifier.Subscribe(team.Value.Id, handler);
        }

        var self = actor.Value;

        return notifier.Subscribe(
            team.Value.Id,
            notification => notification.ActorUserId == self ? Task.CompletedTask : handler(notification));
    }
}
