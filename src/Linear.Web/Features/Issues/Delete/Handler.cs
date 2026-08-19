using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Delete;

/// <summary>
/// Elimina un issue de forma definitiva.
/// </summary>
/// <remarks>
/// A diferencia de archivar, esto no se puede deshacer: por eso pide rol Admin u Owner,
/// igual que borrar una label o el equipo mismo, y no el rol Member que alcanza para el
/// resto de las operaciones sobre issues.
/// </remarks>
public sealed class DeleteIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result> HandleAsync(DeleteIssueRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamIssueAccess.RequireAdminAsync(
            teamAccess, dbContext, request.Key, request.Identifier, trackIssue: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure(resolved.Error);
        }

        dbContext.Issues.Remove(resolved.Value.Issue);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
