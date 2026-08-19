using Linear.Domain.Common;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Labels.List;

/// <summary>
/// Lista las labels de un equipo, ordenadas por nombre.
/// </summary>
public sealed class ListLabelsHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<LabelResponse>>> HandleAsync(
        ListLabelsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamLabelAccess.ForReadingAsync(teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<PagedResult<LabelResponse>>(team.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Labels
            .AsNoTracking()
            .Where(label => label.TeamId == team.Value.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        // Se materializan las entidades en lugar de proyectar: el color es un value object
        // mapeado con un conversor y leer dentro de él no es traducible a SQL.
        var labels = await query
            .OrderBy(label => label.NormalizedName)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToArrayAsync(cancellationToken);

        var items = labels.Select(LabelResponseMapper.ToResponse).ToArray();

        return Result.Success(PagedResult<LabelResponse>.Create(items, page, totalCount));
    }
}
