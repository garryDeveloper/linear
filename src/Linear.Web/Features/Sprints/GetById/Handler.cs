using Linear.Domain.Common;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.GetById;

/// <summary>
/// Devuelve un sprint con sus issues y sus métricas: es lo que alimenta el tablero.
/// </summary>
public sealed class GetSprintByIdHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        GetSprintByIdRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamSprintAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.SprintId, trackSprint: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<SprintResponse>(resolved.Error);
        }

        return Result.Success(
            await SprintResponseMapper.ToResponseAsync(resolved.Value.Sprint, dbContext, cancellationToken));
    }
}
