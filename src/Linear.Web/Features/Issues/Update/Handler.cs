using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Update;

/// <summary>
/// Cambia el título y la descripción de un issue.
/// </summary>
public sealed class UpdateIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        UpdateIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamIssueAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.Identifier, trackIssue: true, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<IssueResponse>(resolved.Error);
        }

        var issue = resolved.Value.Issue;

        // Control de concurrencia: si el issue cambió desde que quien edita lo cargó, no se
        // pisa. La tolerancia de un milisegundo la impone el viaje de ida y vuelta: la
        // versión se serializa a JSON y PostgreSQL guarda microsegundos, así que exigir
        // igualdad exacta rechazaría guardados legítimos por diferencias de redondeo.
        if (request.ExpectedUpdatedAt is { } expected &&
            (issue.UpdatedAt - expected).Duration() > TimeSpan.FromMilliseconds(1))
        {
            return Result.Failure<IssueResponse>(IssueErrors.ModifiedByAnother);
        }

        var updated = issue.UpdateContent(request.Title, request.Description, DateTimeOffset.UtcNow);

        if (updated.IsFailure)
        {
            return Result.Failure<IssueResponse>(updated.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
