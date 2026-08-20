using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.RemoveIssue;

/// <summary>
/// Desasocia un issue de una iniciativa. El issue no se elimina ni se archiva.
/// </summary>
public sealed class RemoveRoadmapItemIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        RemoveRoadmapItemIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamRoadmapAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.RoadmapId, trackRoadmap: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(resolved.Error);
        }

        var (team, roadmap) = resolved.Value;

        var normalized = IssueRoute.NormalizeIdentifier(request.Identifier);

        if (normalized.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(normalized.Error);
        }

        var identifierValue = IssueIdentifier.FromPersistence(normalized.Value);

        // Acotado a la iniciativa además del equipo: pedir por la iniciativa equivocada un
        // issue que aporta a otra no lo desasocia, responde que no está ahí.
        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.TeamId == team.Id &&
                    candidate.RoadmapItemId == request.ItemId &&
                    candidate.Identifier == identifierValue,
                cancellationToken);

        if (issue is null)
        {
            return Result.Failure<RoadmapResponse>(IssueErrors.NotInARoadmapItem);
        }

        var removed = issue.RemoveFromRoadmapItem(DateTimeOffset.UtcNow);

        if (removed.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(removed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap, dbContext, cancellationToken));
    }
}
