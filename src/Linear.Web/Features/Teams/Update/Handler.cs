using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Features.Teams.Update;

/// <summary>
/// Cambia el nombre y la descripción de un equipo.
/// </summary>
/// <remarks>
/// La clave no se puede modificar: forma parte del identificador de cada issue.
/// </remarks>
public sealed class UpdateTeamHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    AppDbContext dbContext)
{
    public async Task<Result<TeamResponse>> HandleAsync(
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var teamKey = TeamKeyRoute.Parse(request.Key);

        if (teamKey.IsFailure)
        {
            return Result.Failure<TeamResponse>(teamKey.Error);
        }

        var team = await teamAccess.RequireRoleAsync(
            teamKey.Value,
            TeamRole.Admin,
            tracking: true,
            cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<TeamResponse>(team.Error);
        }

        var update = team.Value.Update(request.Name, request.Description, DateTimeOffset.UtcNow);

        if (update.IsFailure)
        {
            return Result.Failure<TeamResponse>(update.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await TeamResponseMapper.ToResponseAsync(
            team.Value,
            (await currentUser.RequireIdAsync(cancellationToken)).Value,
            dbContext,
            cancellationToken));
    }
}
