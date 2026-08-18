using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Features.Teams.GetByKey;

/// <summary>
/// Devuelve un equipo con su plantel de miembros.
/// </summary>
public sealed class GetTeamByKeyHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    AppDbContext dbContext)
{
    public async Task<Result<TeamResponse>> HandleAsync(string? key, CancellationToken cancellationToken)
    {
        var teamKey = TeamKeyRoute.Parse(key);

        if (teamKey.IsFailure)
        {
            return Result.Failure<TeamResponse>(teamKey.Error);
        }

        var team = await teamAccess.RequireRoleAsync(
            teamKey.Value,
            TeamRole.Member,
            tracking: false,
            cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<TeamResponse>(team.Error);
        }

        return Result.Success(await TeamResponseMapper.ToResponseAsync(
            team.Value,
            (await currentUser.RequireIdAsync(cancellationToken)).Value,
            dbContext,
            cancellationToken));
    }
}
