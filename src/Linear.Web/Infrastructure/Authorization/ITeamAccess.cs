using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Infrastructure.Authorization;

/// <summary>
/// Comprueba que el usuario en curso pertenezca a un equipo con el rol suficiente.
/// </summary>
/// <remarks>
/// La autorización por equipo se resuelve acá y no con una política de ASP.NET Core porque
/// depende del equipo concreto que la operación toca, y porque los handlers pueden
/// invocarse sin pasar por HTTP. Un guardia atado al enrutado dejaría esos caminos sin
/// proteger, y sostener la misma regla en dos lugares es la forma segura de que se
/// separen con el tiempo.
/// </remarks>
public interface ITeamAccess
{
    /// <summary>
    /// Devuelve el equipo con sus miembros si el usuario en curso tiene al menos
    /// <paramref name="minimumRole"/>.
    /// </summary>
    /// <param name="dbContext">
    /// Contexto de la operación. Lo aporta quien llama para que la entidad devuelta quede
    /// rastreada por el mismo contexto que después va a guardar los cambios.
    /// </param>
    /// <param name="tracking">
    /// Desactivarlo para operaciones de solo lectura; las que modifican el agregado lo
    /// necesitan activo.
    /// </param>
    Task<Result<Team>> RequireRoleAsync(
        AppDbContext dbContext,
        Guid teamId,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken);

    /// <inheritdoc cref="RequireRoleAsync(AppDbContext, Guid, TeamRole, bool, CancellationToken)"/>
    Task<Result<Team>> RequireRoleAsync(
        AppDbContext dbContext,
        TeamKey key,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken);

    /// <summary>
    /// Igual que las anteriores, pero con la identidad dada en lugar de deducida.
    /// </summary>
    /// <remarks>
    /// Existe por el hub de tiempo real. Una conexión de SignalR no tiene <c>HttpContext</c>
    /// mientras se invocan sus métodos —solo lo tuvo durante el handshake— ni vive dentro de
    /// un circuito Blazor, así que <c>ICurrentUser</c> no encuentra a nadie. El hub sí conoce
    /// al usuario: lo tiene en <c>Context.User</c>.
    /// <para>
    /// Se agrega una sobrecarga en vez de que el hub consulte la membresía por su cuenta para
    /// que la política de responder "no existe" a quien no pertenece —y no "no podés"— siga
    /// escrita en un solo lugar.
    /// </para>
    /// </remarks>
    Task<Result<Team>> RequireRoleAsync(
        AppDbContext dbContext,
        TeamKey key,
        Guid userId,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken);
}
