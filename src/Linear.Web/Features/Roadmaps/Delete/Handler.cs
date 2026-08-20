using Linear.Domain.Common;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.Delete;

/// <summary>
/// Elimina un roadmap con todas sus iniciativas.
/// </summary>
/// <remarks>
/// No se puede deshacer y se lleva la planificación de todo el equipo, así que pide rol
/// Admin u Owner — el mismo criterio que eliminar un issue, una label o el equipo mismo.
///
/// Los issues asociados no se borran: la clave foránea los desasocia (<c>SetNull</c>). El
/// trabajo sobrevive al plan que lo agrupaba.
/// </remarks>
public sealed class DeleteRoadmapHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(DeleteRoadmapRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamRoadmapAccess.RequireAdminAsync(
            teamAccess, dbContext, request.Key, request.RoadmapId, trackRoadmap: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure(resolved.Error);
        }

        dbContext.Roadmaps.Remove(resolved.Value.Roadmap);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
