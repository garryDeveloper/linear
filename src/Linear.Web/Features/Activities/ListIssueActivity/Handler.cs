using Linear.Domain.Activities;
using Linear.Domain.Common;
using Linear.Web.Features.Activities.Contracts;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Activities;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Activities.ListIssueActivity;

/// <summary>
/// Historial de un issue, de lo más nuevo a lo más viejo.
/// </summary>
/// <remarks>
/// Incluye dos cosas: lo que pasó sobre el issue mismo —se creó, cambió de estado, se asignó,
/// se le pusieron labels— y lo que pasó en sus comentarios. Lo segundo no se puede pedir por
/// <c>EntityId</c>, porque una actividad de comentario apunta al comentario; se pide por el
/// <c>issueId</c> que viaja dentro del payload.
///
/// Es la razón por la que el payload se guarda como <c>jsonb</c> y no como texto: permite
/// filtrar por dentro sin agregarle a la tabla una columna que la task 011 no define.
/// </remarks>
public sealed class ListIssueActivityHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<PagedResult<ActivityResponse>>> HandleAsync(
        ListIssueActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamIssueAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.Identifier, trackIssue: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<PagedResult<ActivityResponse>>(resolved.Error);
        }

        var (team, issue) = resolved.Value;
        var page = request.ToPageRequest();

        // Fragmento de contención: `@>` en jsonb, que además puede entrar por el índice GIN
        // del payload. El identificador viaja como parámetro, no concatenado en el SQL.
        var belongsToIssue = ActivityPayload.IssueFragment(issue.Id);

        var query = dbContext.Activities
            .AsNoTracking()
            .Where(activity =>
                activity.TeamId == team.Id &&
                ((activity.EntityType == ActivityEntityType.Issue && activity.EntityId == issue.Id) ||
                 EF.Functions.JsonContains(activity.PayloadJson, belongsToIssue)));

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
