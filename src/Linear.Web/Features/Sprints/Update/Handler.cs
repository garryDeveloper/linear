using Linear.Domain.Common;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Update;

/// <summary>
/// Cambia el nombre, el objetivo y las fechas de un sprint.
/// </summary>
public sealed class UpdateSprintHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        UpdateSprintRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamSprintAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.SprintId, trackSprint: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SprintResponse>(resolved.Error);
        }

        var sprint = resolved.Value.Sprint;

        var updated = sprint.Update(
            request.Name, request.Goal, request.StartDate, request.EndDate, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<SprintResponse>(updated.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }
}
