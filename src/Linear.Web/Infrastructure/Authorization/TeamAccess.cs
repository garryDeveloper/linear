using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Authorization;

public sealed class TeamAccess(AppDbContext dbContext, ICurrentUser currentUser) : ITeamAccess
{
    public Task<Result<Team>> RequireRoleAsync(
        Guid teamId,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken) =>
        EvaluateAsync(
            query => query.FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken),
            () => TeamErrors.NotFound(teamId),
            minimumRole,
            tracking,
            cancellationToken);

    public Task<Result<Team>> RequireRoleAsync(
        TeamKey key,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        return EvaluateAsync(
            query => query.FirstOrDefaultAsync(team => team.Key == key, cancellationToken),
            () => TeamErrors.NotFoundByKey(key.Value),
            minimumRole,
            tracking,
            cancellationToken);
    }

    private async Task<Result<Team>> EvaluateAsync(
        Func<IQueryable<Team>, Task<Team?>> find,
        Func<Error> notFound,
        TeamRole minimumRole,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<Team>(userId.Error);
        }

        var query = dbContext.Teams.Include(team => team.Members);

        var team = await find(tracking ? query : query.AsNoTracking());

        // A quien no pertenece al equipo se le responde igual que si el equipo no
        // existiera: distinguir ambos casos permitiría averiguar qué equipos hay.
        if (team is null || team.RoleOf(userId.Value) is not { } role)
        {
            return Result.Failure<Team>(notFound());
        }

        return role >= minimumRole
            ? Result.Success(team)
            : Result.Failure<Team>(TeamErrors.InsufficientRole);
    }
}
