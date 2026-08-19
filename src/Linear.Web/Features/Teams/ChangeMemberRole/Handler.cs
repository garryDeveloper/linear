using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Teams.ChangeMemberRole;

/// <summary>
/// Cambia el rol de un miembro dentro del equipo.
/// </summary>
public sealed class ChangeTeamMemberRoleHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<TeamResponse>> HandleAsync(
        ChangeTeamMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(request);

        var teamKey = TeamKeyRoute.Parse(request.Key);

        if (teamKey.IsFailure)
        {
            return Result.Failure<TeamResponse>(teamKey.Error);
        }

        var team = await teamAccess.RequireRoleAsync(
            dbContext,
            teamKey.Value,
            TeamRole.Admin,
            tracking: true,
            cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<TeamResponse>(team.Error);
        }

        var actingUserId = (await currentUser.RequireIdAsync(cancellationToken)).Value;

        // Alcanza con que el rol actual o el nuevo sea Owner para exigir ser Owner:
        // así un Admin no puede ni degradar a un Owner ni promover a nadie a Owner.
        var ownership = TeamOwnershipRules.EnsureOwnerPrivilege(
            team.Value,
            actingUserId,
            currentTargetRole: team.Value.RoleOf(request.UserId),
            newTargetRole: request.Role);

        if (ownership.IsFailure)
        {
            return Result.Failure<TeamResponse>(ownership.Error);
        }

        var changed = team.Value.ChangeMemberRole(request.UserId, request.Role, DateTimeOffset.UtcNow);

        if (changed.IsFailure)
        {
            return Result.Failure<TeamResponse>(changed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await TeamResponseMapper.ToResponseAsync(
            team.Value,
            actingUserId,
            dbContext,
            cancellationToken));
    }
}
