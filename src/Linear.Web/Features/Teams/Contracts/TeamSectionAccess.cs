using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Features.Teams.Contracts;

/// <summary>
/// Resuelve el equipo de la ruta y comprueba el rol mínimo necesario para operar dentro de
/// él — labels, issues, o cualquier otra sección con el mismo umbral.
/// </summary>
/// <remarks>
/// Lo comparten los slices de Labels e Issues, que además garantiza el aislamiento entre
/// equipos: nunca se consulta una label o un issue sin haber verificado antes el acceso a
/// su equipo.
/// </remarks>
public static class TeamSectionAccess
{
    /// <summary>Cualquier miembro del equipo. Alcanza para leer y para el trabajo del día a día.</summary>
    public static Task<Result<Team>> RequireMemberAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        CancellationToken cancellationToken) =>
        ResolveAsync(teamAccess, dbContext, teamKey, TeamRole.Member, cancellationToken);

    /// <summary>Admin u Owner. Para administrar la configuración del equipo o borrar algo definitivamente.</summary>
    public static Task<Result<Team>> RequireAdminAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        CancellationToken cancellationToken) =>
        ResolveAsync(teamAccess, dbContext, teamKey, TeamRole.Admin, cancellationToken);

    private static async Task<Result<Team>> ResolveAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        TeamRole minimumRole,
        CancellationToken cancellationToken)
    {
        var key = TeamKeyRoute.Parse(teamKey);

        if (key.IsFailure)
        {
            return Result.Failure<Team>(key.Error);
        }

        return await teamAccess.RequireRoleAsync(
            dbContext,
            key.Value,
            minimumRole,
            tracking: false,
            cancellationToken);
    }
}
