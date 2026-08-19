using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Update;

/// <summary>
/// Cambia el título y la descripción de un issue.
/// </summary>
public sealed class UpdateIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        UpdateIssueRequest request,
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

        var updated = issue.UpdateContent(request.Title, request.Description, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<IssueResponse>(updated.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
