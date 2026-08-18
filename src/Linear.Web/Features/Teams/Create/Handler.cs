using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Linear.Web.Features.Teams.Create;

/// <summary>
/// Crea un equipo y deja a quien lo creó como su Owner.
/// </summary>
public sealed class CreateTeamHandler(
    AppDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<CreateTeamHandler> logger)
{
    /// <summary>Código de PostgreSQL para una violación de restricción única.</summary>
    private const string UniqueViolation = "23505";

    public async Task<Result<TeamResponse>> HandleAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<TeamResponse>(userId.Error);
        }

        var key = TeamKey.Create(request.Key);

        if (key.IsFailure)
        {
            return Result.Failure<TeamResponse>(key.Error);
        }

        var team = Team.Create(request.Name, key.Value, request.Description, userId.Value, DateTimeOffset.UtcNow);

        if (team.IsFailure)
        {
            return Result.Failure<TeamResponse>(team.Error);
        }

        dbContext.Teams.Add(team.Value);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            // Consultar primero si la clave existe no alcanza: dos peticiones simultáneas
            // pasarían ambas esa comprobación. El índice único de la base es el árbitro.
            logger.LogInformation("Se rechazó el equipo con clave duplicada '{Key}'.", key.Value.Value);

            return Result.Failure<TeamResponse>(TeamErrors.KeyAlreadyExists);
        }

        return Result.Success(
            await TeamResponseMapper.ToResponseAsync(team.Value, userId.Value, dbContext, cancellationToken));
    }

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
