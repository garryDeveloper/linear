using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.RemoveIssue;

/// <summary>
/// Saca un issue del sprint. El issue queda sin sprint; no se elimina ni se archiva.
/// </summary>
public sealed class RemoveSprintIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        RemoveSprintIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamSprintAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.SprintId, trackSprint: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SprintResponse>(resolved.Error);
        }

        var (team, sprint) = resolved.Value;

        if (sprint.IsClosed)
        {
            return Result.Failure<SprintResponse>(SprintErrors.Closed);
        }

        var normalized = IssueRoute.NormalizeIdentifier(request.Identifier);

        if (normalized.IsFailure)
        {
            return Result.Failure<SprintResponse>(normalized.Error);
        }

        var identifierValue = IssueIdentifier.FromPersistence(normalized.Value);

        // Acotado al sprint además de al equipo: pedir por el sprint equivocado un issue que
        // está en otro no lo saca de donde está, responde que no existe ahí.
        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.TeamId == team.Id &&
                    candidate.SprintId == sprint.Id &&
                    candidate.Identifier == identifierValue,
                cancellationToken);

        if (issue is null)
        {
            return Result.Failure<SprintResponse>(IssueErrors.NotInASprint);
        }

        var removed = issue.RemoveFromSprint(DateTimeOffset.UtcNow);

        if (removed.IsFailure)
        {
            return Result.Failure<SprintResponse>(removed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }
}
