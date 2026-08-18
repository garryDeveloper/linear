using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Teams.AddMember;

/// <summary>
/// Suma un usuario existente al equipo.
/// </summary>
public sealed class AddTeamMemberHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    AppDbContext dbContext)
{
    public async Task<Result<TeamResponse>> HandleAsync(
        AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var teamKey = TeamKeyRoute.Parse(request.Key);

        if (teamKey.IsFailure)
        {
            return Result.Failure<TeamResponse>(teamKey.Error);
        }

        var email = Email.Create(request.Email);

        if (email.IsFailure)
        {
            return Result.Failure<TeamResponse>(email.Error);
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
            currentTargetRole: null,
            newTargetRole: request.Role);

        if (ownership.IsFailure)
        {
            return Result.Failure<TeamResponse>(ownership.Error);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email == email.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<TeamResponse>(TeamMemberErrors.UserNotFound(email.Value.Value));
        }

        if (!user.IsActive)
        {
            return Result.Failure<TeamResponse>(TeamMemberErrors.UserInactive);
        }

        var added = team.Value.AddMember(user.Id, request.Role, DateTimeOffset.UtcNow);

        if (added.IsFailure)
        {
            return Result.Failure<TeamResponse>(added.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await TeamResponseMapper.ToResponseAsync(
            team.Value,
            actingUserId,
            dbContext,
            cancellationToken));
    }
}
