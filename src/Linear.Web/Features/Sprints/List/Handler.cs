using Linear.Domain.Common;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.List;

/// <summary>
/// Lista los sprints de un equipo, del más reciente al más viejo.
/// </summary>
/// <remarks>
/// Ordenados por fecha de inicio descendente: el sprint en curso y los que vienen son lo
/// que se mira todos los días; los cerrados quedan atrás, como historia.
/// </remarks>
public sealed class ListSprintsHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<SprintSummaryResponse>>> HandleAsync(
        ListSprintsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<PagedResult<SprintSummaryResponse>>(team.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Sprints
            .AsNoTracking()
            .Where(sprint => sprint.TeamId == team.Value.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var sprints = await query
            .OrderByDescending(sprint => sprint.StartDate)
            .ThenByDescending(sprint => sprint.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToArrayAsync(cancellationToken);

        var items = await SprintResponseMapper.ToSummariesAsync(sprints, dbContext, cancellationToken);

        return Result.Success(PagedResult<SprintSummaryResponse>.Create(items, page, totalCount));
    }
}
