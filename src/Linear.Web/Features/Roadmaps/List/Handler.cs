using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.List;

/// <summary>
/// Lista los roadmaps de un equipo, del más nuevo al más viejo.
/// </summary>
/// <remarks>
/// El listado no trae las iniciativas: solo cuánto tiene cada roadmap. Cargarlas con un
/// <c>Include</c> sobre una consulta paginada duplicaría filas —el problema de siempre—, y
/// la lista tampoco las dibuja: eso es trabajo de la línea de tiempo.
/// </remarks>
public sealed class ListRoadmapsHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<RoadmapSummaryResponse>>> HandleAsync(
        ListRoadmapsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<PagedResult<RoadmapSummaryResponse>>(team.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Roadmaps
            .AsNoTracking()
            .Where(roadmap => roadmap.TeamId == team.Value.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        // La cantidad de iniciativas se proyecta en la misma consulta, sin traerlas.
        var rows = await query
            .OrderByDescending(roadmap => roadmap.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(roadmap => new
            {
                roadmap.Id,
                roadmap.Name,
                roadmap.Description,
                ItemCount = roadmap.Items.Count,
                roadmap.CreatedAt,
                roadmap.UpdatedAt
            })
            .ToArrayAsync(cancellationToken);

        var items = rows
            .Select(row => new RoadmapSummaryResponse(
                row.Id, row.Name, row.Description, row.ItemCount, row.CreatedAt, row.UpdatedAt))
            .ToArray();

        return Result.Success(PagedResult<RoadmapSummaryResponse>.Create(items, page, totalCount));
    }
}
