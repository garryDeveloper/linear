using Linear.Domain.Common;
using Linear.Domain.Labels;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Linear.Web.Features.Labels.Create;

/// <summary>
/// Crea una label dentro de un equipo.
/// </summary>
public sealed class CreateLabelHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<CreateLabelHandler> logger)
{
    /// <summary>Código de PostgreSQL para una violación de restricción única.</summary>
    private const string UniqueViolation = "23505";

    public async Task<Result<LabelResponse>> HandleAsync(
        CreateLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamLabelAccess.ForManagingAsync(teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<LabelResponse>(team.Error);
        }

        var color = ResolveColor(request.Color);

        if (color.IsFailure)
        {
            return Result.Failure<LabelResponse>(color.Error);
        }

        var label = Label.Create(
            team.Value.Id,
            request.Name,
            request.Description,
            color.Value,
            DateTimeOffset.UtcNow);

        if (label.IsFailure)
        {
            return Result.Failure<LabelResponse>(label.Error);
        }

        dbContext.Labels.Add(label.Value);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateName(exception))
        {
            // El índice único es el árbitro: dos peticiones simultáneas con el mismo
            // nombre pasarían ambas una comprobación previa en memoria.
            logger.LogInformation(
                "Se rechazó la label duplicada '{Name}' en el equipo {TeamId}.",
                request.Name,
                team.Value.Id);

            return Result.Failure<LabelResponse>(LabelErrors.NameAlreadyExists);
        }

        return Result.Success(LabelResponseMapper.ToResponse(label.Value));
    }

    /// <summary>
    /// Un color vacío no es un error: significa que el usuario no eligió ninguno.
    /// </summary>
    internal static Result<LabelColor> ResolveColor(string? color) =>
        string.IsNullOrWhiteSpace(color)
            ? Result.Success(LabelColor.Default)
            : LabelColor.Create(color);

    internal static bool IsDuplicateName(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
