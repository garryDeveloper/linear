using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.CreateItem;

/// <summary>
/// Suma una iniciativa al roadmap.
/// </summary>
/// <remarks>
/// Pasa por la raíz del agregado y no crea la iniciativa por su cuenta: es <c>Roadmap</c>
/// quien la valida y la incorpora.
/// </remarks>
public sealed class CreateRoadmapItemHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        CreateRoadmapItemRequest request,
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

        var item = roadmap.AddItem(
            request.Name,
            request.Description,
            request.StartDate,
            request.TargetDate,
            DateTimeOffset.UtcNow);

        if (item.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(item.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap, dbContext, cancellationToken));
    }
}
