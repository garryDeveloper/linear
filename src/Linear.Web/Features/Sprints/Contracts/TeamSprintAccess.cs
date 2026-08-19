using Linear.Domain.Common;
using Linear.Domain.Sprints;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Contracts;

/// <summary>
/// Resuelve el equipo y el sprint que identifica la ruta.
/// </summary>
/// <remarks>
/// Igual que <c>TeamIssueAccess</c>: primero el equipo —que ya responde 404 sin distinguir
/// "no existe" de "no tenés acceso"— y después el sprint acotado a ese equipo ya
/// autorizado. Pedir el sprint de otro equipo por la ruta equivocada nunca lo encuentra.
///
/// Planificar el trabajo del equipo es trabajo del día a día, no configuración: alcanza con
/// ser miembro, igual que para crear o mover un issue.
/// </remarks>
public static class TeamSprintAccess
{
    public static async Task<Result<(Team Team, Sprint Sprint)>> RequireMemberAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        Guid sprintId,
        bool trackSprint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, teamKey, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<(Team, Sprint)>(team.Error);
        }

        var query = dbContext.Sprints
            .Where(candidate => candidate.Id == sprintId && candidate.TeamId == team.Value.Id);

        var sprint = await (trackSprint ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        return sprint is null
            ? Result.Failure<(Team, Sprint)>(SprintErrors.NotFound(sprintId))
            : Result.Success((team.Value, sprint));
    }
}
