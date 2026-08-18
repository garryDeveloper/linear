using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Features.Teams.RemoveMember;

/// <summary>
/// Quita a un usuario del equipo.
/// </summary>
public sealed class RemoveTeamMemberHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    AppDbContext dbContext)
{
    public async Task<Result<TeamResponse>> HandleAsync(
        RemoveTeamMemberRequest request,
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

        var actingUserId = (await currentUser.RequireIdAsync(cancellationToken)).Value;

        var ownership = TeamOwnershipRules.EnsureOwnerPrivilege(
            team.Value,
            actingUserId,
            currentTargetRole: team.Value.RoleOf(request.UserId),
            newTargetRole: null);

        if (ownership.IsFailure)
        {
            return Result.Failure<TeamResponse>(ownership.Error);
        }

        var removed = team.Value.RemoveMember(request.UserId, DateTimeOffset.UtcNow);

        if (removed.IsFailure)
        {
            return Result.Failure<TeamResponse>(removed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await TeamResponseMapper.ToResponseAsync(
            team.Value,
            actingUserId,
            dbContext,
            cancellationToken));
    }
}
