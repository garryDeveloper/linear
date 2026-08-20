using Linear.Domain.Common;
using Linear.Web.Features.Activities.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Activities.ListTeamActivity;

/// <summary>
/// Historial reciente de un equipo, de lo más nuevo a lo más viejo.
/// </summary>
/// <remarks>
/// Entra por el índice <c>(TeamId, CreatedAt)</c>, que es exactamente la forma de esta
/// consulta.
/// </remarks>
public sealed class ListTeamActivityHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<ActivityResponse>>> HandleAsync(
        ListTeamActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<PagedResult<ActivityResponse>>(team.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Activities
            .AsNoTracking()
            .Where(activity => activity.TeamId == team.Value.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var activities = await query
            .OrderByDescending(activity => activity.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToArrayAsync(cancellationToken);

        var items = await ActivityResponseMapper.ToResponsesAsync(activities, dbContext, cancellationToken);

        return Result.Success(PagedResult<ActivityResponse>.Create(items, page, totalCount));
    }
}
