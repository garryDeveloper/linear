using Linear.Domain.Common;
using Linear.Domain.Labels;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Features.Labels.Create;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Labels.Update;

/// <summary>
/// Cambia el nombre, la descripción y el color de una label.
/// </summary>
public sealed class UpdateLabelHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<LabelResponse>> HandleAsync(
        UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamLabelAccess.ForManagingAsync(teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<LabelResponse>(team.Error);
        }

        var color = CreateLabelHandler.ResolveColor(request.Color);

        if (color.IsFailure)
        {
            return Result.Failure<LabelResponse>(color.Error);
        }

        // La label se busca acotada al equipo ya autorizado: pedir una de otro equipo
        // devuelve "no existe" en lugar de tocarla.
        var label = await dbContext.Labels.FirstOrDefaultAsync(
            candidate => candidate.Id == request.LabelId && candidate.TeamId == team.Value.Id,
            cancellationToken);

        if (label is null)
        {
            return Result.Failure<LabelResponse>(LabelErrors.NotFound(request.LabelId));
        }

        var updated = label.Update(request.Name, request.Description, color.Value, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<LabelResponse>(updated.Error);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (CreateLabelHandler.IsDuplicateName(exception))
        {
            return Result.Failure<LabelResponse>(LabelErrors.NameAlreadyExists);
        }

        return Result.Success(LabelResponseMapper.ToResponse(label));
    }
}
