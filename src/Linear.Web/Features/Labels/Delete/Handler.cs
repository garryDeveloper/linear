using Linear.Domain.Common;
using Linear.Domain.Labels;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Labels.Delete;

/// <summary>
/// Elimina una label del equipo.
/// </summary>
/// <remarks>
/// Cuando existan issues etiquetados, borrar la label solo debe quitar la asociación, no
/// los issues: eso lo resuelve la tabla de relación que llega con la task 005.
/// </remarks>
public sealed class DeleteLabelHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(DeleteLabelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamLabelAccess.ForManagingAsync(teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure(team.Error);
        }

        var label = await dbContext.Labels.FirstOrDefaultAsync(
            candidate => candidate.Id == request.LabelId && candidate.TeamId == team.Value.Id,
            cancellationToken);

        if (label is null)
        {
            return Result.Failure(LabelErrors.NotFound(request.LabelId));
        }

        dbContext.Labels.Remove(label);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
