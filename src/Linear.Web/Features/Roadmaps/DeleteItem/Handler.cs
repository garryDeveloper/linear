using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.DeleteItem;

/// <summary>
/// Elimina una iniciativa del roadmap.
/// </summary>
/// <remarks>
/// Es definitivo, así que pide rol Admin u Owner igual que eliminar el roadmap entero. Los
/// issues asociados quedan sin iniciativa, no se borran.
/// </remarks>
public sealed class DeleteRoadmapItemHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(
        DeleteRoadmapItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamRoadmapAccess.RequireAdminAsync(
            teamAccess, dbContext, request.Key, request.RoadmapId, trackRoadmap: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure(resolved.Error);
        }

        var removed = resolved.Value.Roadmap.RemoveItem(request.ItemId, DateTimeOffset.UtcNow);

        if (removed.IsFailure)
        {
            return removed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
