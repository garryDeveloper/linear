using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Realtime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Realtime;

/// <summary>
/// Hub de tiempo real, con el equipo como unidad de contexto.
/// </summary>
/// <remarks>
/// Un cliente se conecta una vez y se suscribe a los equipos que le interesan; cada equipo
/// es un grupo de SignalR. El aislamiento no depende de que el cliente filtre lo que recibe:
/// los avisos se emiten al grupo, y a un grupo solo se entra demostrando que se pertenece
/// al equipo.
/// <para>
/// La comprobación se hace al suscribirse y no al emitir. Emitir ocurre una vez por cambio y
/// tiene que ser barato; suscribirse ocurre una vez por pantalla. Hacerlo al revés
/// significaría consultar la membresía de cada destinatario en cada evento.
/// </para>
/// </remarks>
[Authorize]
public sealed class TeamHub(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory) : Hub<ITeamClient>
{
    /// <summary>Ruta donde se publica el hub.</summary>
    public const string Route = "/hubs/team";

    /// <summary>
    /// Nombre del grupo de un equipo.
    /// </summary>
    /// <remarks>
    /// Por identificador y no por clave: la clave de un equipo se puede cambiar, y una
    /// conexión suscripta con la clave vieja seguiría recibiendo —o dejaría de recibir—
    /// según el orden en que ocurrieran las cosas.
    /// </remarks>
    public static string GroupFor(Guid teamId) => $"team:{teamId}";

    /// <summary>
    /// Suscribe la conexión a los cambios de un equipo.
    /// </summary>
    /// <returns>
    /// <c>true</c> si quedó suscripta. Se devuelve un booleano en lugar de lanzar porque no
    /// pertenecer a un equipo no es un error del programa: es la respuesta esperada cuando
    /// alguien pide un equipo que no es suyo.
    /// </returns>
    public async Task<bool> SubscribeAsync(string teamKey)
    {
        var resolved = await ResolveAsync(teamKey);

        if (resolved is not { } teamId)
        {
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(teamId));

        return true;
    }

    /// <summary>
    /// Da de baja la suscripción.
    /// </summary>
    /// <remarks>
    /// También comprueba la membresía, aunque salir de un grupo sea inofensivo: sin la
    /// comprobación, este método diría qué claves de equipo existen a quien las pruebe.
    /// </remarks>
    public async Task UnsubscribeAsync(string teamKey)
    {
        var resolved = await ResolveAsync(teamKey);

        if (resolved is { } teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(teamId));
        }
    }

    /// <summary>
    /// Devuelve el equipo si quien llama pertenece a él, y nulo en cualquier otro caso.
    /// </summary>
    private async Task<Guid?> ResolveAsync(string teamKey)
    {
        // La identidad la trae la conexión. El hub no usa ICurrentUser porque durante la
        // invocación de un método no hay HttpContext del que deducirla.
        if (Context.User.GetUserId() is not { } userId)
        {
            return null;
        }

        var key = TeamKey.Create(teamKey);

        if (key.IsFailure)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(Context.ConnectionAborted);

        // Alcanza con ser miembro: recibir avisos es leer, y leer es lo que cualquier
        // integrante del equipo ya puede hacer por la interfaz.
        var team = await teamAccess.RequireRoleAsync(
            dbContext,
            key.Value,
            userId,
            TeamRole.Member,
            tracking: false,
            Context.ConnectionAborted);

        return team.IsSuccess ? team.Value.Id : null;
    }
}
