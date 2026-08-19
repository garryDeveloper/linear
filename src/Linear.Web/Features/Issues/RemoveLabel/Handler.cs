using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.RemoveLabel;

public sealed class RemoveIssueLabelHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        RemoveIssueLabelRequest request,
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

        var removed = issue.RemoveLabel(request.LabelId, DateTimeOffset.UtcNow);

        if (removed.IsFailure)
        {
            return Result.Failure<IssueResponse>(removed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
