using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.UpdateItem;

/// <summary>
/// Edita una iniciativa: nombre, descripción, fechas y estado.
/// </summary>
public sealed class UpdateRoadmapItemHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        UpdateRoadmapItemRequest request,
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
        var now = DateTimeOffset.UtcNow;

        var updated = roadmap.UpdateItem(
            request.ItemId, request.Name, request.Description, request.StartDate, request.TargetDate, now);

        if (updated.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(updated.Error);
        }

        var status = roadmap.ChangeItemStatus(request.ItemId, request.Status, now);

        if (status.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(status.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap, dbContext, cancellationToken));
    }
}
