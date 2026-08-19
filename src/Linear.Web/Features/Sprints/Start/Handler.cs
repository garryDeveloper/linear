using Linear.Domain.Common;
using Linear.Domain.Sprints;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Linear.Web.Features.Sprints.Start;

/// <summary>
/// Pone un sprint en curso.
/// </summary>
/// <remarks>
/// Acá vive la regla central de la task: un equipo tiene a lo sumo un sprint activo. Se
/// sostiene en dos niveles y los dos hacen falta.
///
/// El chequeo previo existe para dar un error claro en el caso normal —"ya tenés un sprint
/// activo"— sin depender de que falle la base. Pero por sí solo no alcanza: entre leer que
/// no hay ninguno activo y guardar el propio hay una ventana en la que otro pedido puede
/// leer lo mismo, y los dos terminarían activos.
///
/// Quien realmente lo impide es el índice único parcial <see cref="SprintConfiguration"/>:
/// la base solo acepta una fila Active por equipo. Si dos pedidos concurrentes llegan a
/// guardar, uno gana y el otro recibe una violación de unicidad que acá se traduce al mismo
/// error de dominio que habría dado el chequeo. Es el mismo criterio que el número de issue
/// de la task 005: la garantía se apoya en el motor, no en el orden en que corran los
/// pedidos.
/// </remarks>
public sealed class StartSprintHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        StartSprintRequest request,
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

        var (team, sprint) = resolved.Value;

        var alreadyActive = await dbContext.Sprints
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.TeamId == team.Id && candidate.Status == SprintStatus.Active,
                cancellationToken);

        if (alreadyActive)
        {
            return Result.Failure<SprintResponse>(SprintErrors.TeamAlreadyHasAnActiveSprint);
        }

        var started = sprint.Start(DateTimeOffset.UtcNow);

        if (started.IsFailure)
        {
            return Result.Failure<SprintResponse>(started.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveSprintCollision(exception))
        {
            return Result.Failure<SprintResponse>(SprintErrors.TeamAlreadyHasAnActiveSprint);
        }

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }

    private static bool IsActiveSprintCollision(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } postgres &&
        postgres.ConstraintName == SprintConfiguration.OneActiveSprintPerTeamIndex;
}
