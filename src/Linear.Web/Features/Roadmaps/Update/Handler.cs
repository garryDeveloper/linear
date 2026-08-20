using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.Update;

/// <summary>
/// Cambia el nombre y la descripción de un roadmap.
/// </summary>
public sealed class UpdateRoadmapHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        UpdateRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamRoadmapAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.RoadmapId, trackRoadmap: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(resolved.Error);
        }

        var roadmap = resolved.Value.Roadmap;

        var updated = roadmap.Update(request.Name, request.Description, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(updated.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap, dbContext, cancellationToken));
    }
}
