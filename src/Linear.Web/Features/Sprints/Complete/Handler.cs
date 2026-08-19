using Linear.Domain.Common;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Complete;

/// <summary>
/// Cierra un sprint en curso.
/// </summary>
/// <remarks>
/// Los issues que quedaron sin terminar siguen en el sprint: es el registro de qué se
/// comprometió y qué se logró en ese período. Moverlos al siguiente sprint —o sacarlos— es
/// una decisión del equipo, que hace con las operaciones de asignar y remover issues.
/// </remarks>
public sealed class CompleteSprintHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        CompleteSprintRequest request,
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

        var completed = sprint.Complete(DateTimeOffset.UtcNow);

        if (completed.IsFailure)
        {
            return Result.Failure<SprintResponse>(completed.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }
}
