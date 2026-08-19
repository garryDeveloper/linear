using Linear.Domain.Common;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Cancel;

/// <summary>
/// Cancela un sprint, esté planificado o en curso.
/// </summary>
/// <remarks>
/// Cancelar el sprint activo libera el cupo del equipo: el índice único parcial solo cuenta
/// filas en estado Active, así que después de esto se puede iniciar otro.
/// </remarks>
public sealed class CancelSprintHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        CancelSprintRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamSprintAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.SprintId, trackSprint: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SprintResponse>(resolved.Error);
        }

        var sprint = resolved.Value.Sprint;

        var canceled = sprint.Cancel(DateTimeOffset.UtcNow);

        if (canceled.IsFailure)
        {
            return Result.Failure<SprintResponse>(canceled.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }
}
