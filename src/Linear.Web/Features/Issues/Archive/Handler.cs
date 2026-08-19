using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Archive;

/// <summary>
/// Archiva un issue: lo saca del listado activo sin eliminarlo.
/// </summary>
public sealed class ArchiveIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        ArchiveIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamIssueAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.Identifier, trackIssue: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<IssueResponse>(resolved.Error);
        }

        var issue = resolved.Value.Issue;

        var archived = issue.Archive(DateTimeOffset.UtcNow);

        if (archived.IsFailure)
        {
            return Result.Failure<IssueResponse>(archived.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
