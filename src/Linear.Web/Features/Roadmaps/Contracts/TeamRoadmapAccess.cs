using Linear.Domain.Common;
using Linear.Domain.Roadmaps;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.Contracts;

/// <summary>
/// Resuelve el equipo y el roadmap que identifica la ruta.
/// </summary>
/// <remarks>
/// Igual que <c>TeamSprintAccess</c>: primero el equipo —que ya responde 404 sin distinguir
/// "no existe" de "no tenés acceso"— y después el roadmap acotado a ese equipo ya
/// autorizado. Pedir el roadmap de otro equipo por la ruta equivocada nunca lo encuentra.
///
/// Las iniciativas se cargan con el roadmap porque son parte de su agregado: modificarlas
/// pasa siempre por la raíz.
/// </remarks>
public static class TeamRoadmapAccess
{
    /// <summary>Planificar es trabajo del día a día: alcanza con pertenecer al equipo.</summary>
    public static Task<Result<(Team Team, Roadmap Roadmap)>> RequireMemberAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        Guid roadmapId,
        bool trackRoadmap,
        CancellationToken cancellationToken) =>
        ResolveAsync(
            teamAccess,
            dbContext,
            teamKey,
            roadmapId,
            TeamSectionAccess.RequireMemberAsync,
            trackRoadmap,
            cancellationToken);

    /// <summary>
    /// Admin u Owner. Para eliminar, que es definitivo — el mismo criterio que rige para
    /// eliminar un issue, una label o el equipo mismo.
    /// </summary>
    public static Task<Result<(Team Team, Roadmap Roadmap)>> RequireAdminAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        Guid roadmapId,
        bool trackRoadmap,
        CancellationToken cancellationToken) =>
        ResolveAsync(
            teamAccess,
            dbContext,
            teamKey,
            roadmapId,
            TeamSectionAccess.RequireAdminAsync,
            trackRoadmap,
            cancellationToken);

    private static async Task<Result<(Team Team, Roadmap Roadmap)>> ResolveAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        Guid roadmapId,
        Func<ITeamAccess, AppDbContext, string?, CancellationToken, Task<Result<Team>>> resolveTeam,
        bool trackRoadmap,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var team = await resolveTeam(teamAccess, dbContext, teamKey, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<(Team, Roadmap)>(team.Error);
        }

        // Include de las iniciativas: es una consulta de un único roadmap, sin paginación de
        // por medio, así que no aplica el problema de duplicar filas al combinar Include con
        // Skip/Take.
        var query = dbContext.Roadmaps
            .Include(roadmap => roadmap.Items)
            .Where(roadmap => roadmap.Id == roadmapId && roadmap.TeamId == team.Value.Id);

        var found = await (trackRoadmap ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        return found is null
            ? Result.Failure<(Team, Roadmap)>(RoadmapErrors.NotFound(roadmapId))
            : Result.Success((team.Value, found));
    }
}
