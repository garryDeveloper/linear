using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.List;

/// <summary>
/// Lista los issues de un equipo, del más nuevo al más viejo.
/// </summary>
public sealed class ListIssuesHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<IssueSummaryResponse>>> HandleAsync(
        ListIssuesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<PagedResult<IssueSummaryResponse>>(team.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Issues
            .AsNoTracking()
            .Where(issue => issue.TeamId == team.Value.Id);

        if (!request.IncludeArchived)
        {
            query = query.Where(issue => issue.ArchivedAt == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sin Include acá a propósito: un Include sobre Labels se traduce en un JOIN, y un
        // JOIN sobre una consulta con Skip/Take duplica o desalinea la página. Las labels se
        // cargan aparte, ya acotadas a esta página, dentro de ToSummariesAsync.
        var issues = await query
            .OrderByDescending(issue => issue.CreatedAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToArrayAsync(cancellationToken);

        var items = await IssueResponseMapper.ToSummariesAsync(issues, dbContext, cancellationToken);

        return Result.Success(PagedResult<IssueSummaryResponse>.Create(items, page, totalCount));
    }
}
