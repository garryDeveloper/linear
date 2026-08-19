using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.AssignUser;

/// <summary>
/// Asigna un responsable al issue, o lo deja sin asignar.
/// </summary>
public sealed class AssignIssueUserHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        AssignIssueUserRequest request,
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

        var (team, issue) = resolved.Value;

        if (request.AssigneeId is { } assigneeId && !team.HasMember(assigneeId))
        {
            return Result.Failure<IssueResponse>(IssueErrors.AssigneeNotAMember);
        }

        issue.AssignTo(request.AssigneeId, DateTimeOffset.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
