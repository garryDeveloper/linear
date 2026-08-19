using Linear.Domain.Common;
using Linear.Domain.Sprints;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Create;

/// <summary>
/// Crea un sprint planificado dentro de un equipo.
/// </summary>
/// <remarks>
/// Nace siempre en <c>Planned</c>: iniciarlo es un paso aparte, porque es ahí donde entra
/// en juego la regla de un único sprint activo.
/// </remarks>
public sealed class CreateSprintHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        CreateSprintRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<SprintResponse>(team.Error);
        }

        var sprint = Sprint.Create(
            team.Value.Id,
            request.Name,
            request.Goal,
            request.StartDate,
            request.EndDate,
            DateTimeOffset.UtcNow);

        if (sprint.IsFailure)
        {
            return Result.Failure<SprintResponse>(sprint.Error);
        }

        dbContext.Sprints.Add(sprint.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await SprintResponseMapper.ToResponseAsync(sprint.Value, dbContext, cancellationToken));
    }
}
