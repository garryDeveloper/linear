using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Teams.Delete;

/// <summary>
/// Elimina un equipo.
/// </summary>
/// <remarks>
/// Reservado al Owner: arrastra en cascada a los miembros y, más adelante, a los issues,
/// labels y sprints del equipo.
/// </remarks>
public sealed class DeleteTeamHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(string? key, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var teamKey = TeamKeyRoute.Parse(key);

        if (teamKey.IsFailure)
        {
            return Result.Failure(teamKey.Error);
        }

        var team = await teamAccess.RequireRoleAsync(
            dbContext,
            teamKey.Value,
            TeamRole.Owner,
            tracking: true,
            cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure(team.Error);
        }

        dbContext.Teams.Remove(team.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
