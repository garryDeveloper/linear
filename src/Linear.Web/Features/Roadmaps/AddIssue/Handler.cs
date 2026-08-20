using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.AddIssue;

/// <summary>
/// Asocia un issue a una iniciativa del roadmap.
/// </summary>
/// <remarks>
/// Si el issue ya aportaba a otra iniciativa, se mueve: aporta a una sola. A diferencia del
/// sprint, no hay estado que impida asociar — una iniciativa completada o cancelada sigue
/// admitiendo cambios, porque el roadmap es una intención revisable y no el registro cerrado
/// de un período.
/// </remarks>
public sealed class AddRoadmapItemIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        AddRoadmapItemIssueRequest request,
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

        if (roadmap.FindItem(request.ItemId) is null)
        {
            return Result.Failure<RoadmapResponse>(RoadmapErrors.ItemNotFound(request.ItemId));
        }

        var issue = await FindIssueAsync(dbContext, team.Id, request.Identifier, cancellationToken);

        if (issue.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(issue.Error);
        }

        var assigned = issue.Value.AssignToRoadmapItem(request.ItemId, DateTimeOffset.UtcNow);

        if (assigned.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(assigned.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap, dbContext, cancellationToken));
    }

    /// <summary>
    /// Busca el issue dentro del equipo ya autorizado. Que la búsqueda esté acotada por
    /// <c>TeamId</c> es lo que impide asociar el issue de otro equipo.
    /// </summary>
    private static async Task<Result<Issue>> FindIssueAsync(
        AppDbContext dbContext,
        Guid teamId,
        string? identifier,
        CancellationToken cancellationToken)
    {
        var normalized = IssueRoute.NormalizeIdentifier(identifier);

        if (normalized.IsFailure)
        {
            return Result.Failure<Issue>(normalized.Error);
        }

        var identifierValue = IssueIdentifier.FromPersistence(normalized.Value);

        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(
                candidate => candidate.TeamId == teamId && candidate.Identifier == identifierValue,
                cancellationToken);

        return issue is null
            ? Result.Failure<Issue>(IssueErrors.NotFound(normalized.Value))
            : Result.Success(issue);
    }
}
