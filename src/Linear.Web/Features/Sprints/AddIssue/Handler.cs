using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.AddIssue;

/// <summary>
/// Suma un issue al sprint.
/// </summary>
/// <remarks>
/// Si el issue ya estaba en otro sprint, se mueve: un issue pertenece a un único sprint, así
/// que no hace falta sacarlo del anterior primero.
///
/// No se admite sumar issues a un sprint cerrado: un sprint completado o cancelado es el
/// registro de lo que pasó en ese período, y cambiarlo después reescribiría esa historia.
/// </remarks>
public sealed class AddSprintIssueHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<SprintResponse>> HandleAsync(
        AddSprintIssueRequest request,
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

        var (team, sprint) = resolved.Value;

        if (sprint.IsClosed)
        {
            return Result.Failure<SprintResponse>(SprintErrors.Closed);
        }

        var issue = await FindIssueAsync(dbContext, team.Id, request.Identifier, cancellationToken);

        if (issue.IsFailure)
        {
            return Result.Failure<SprintResponse>(issue.Error);
        }

        var assigned = issue.Value.AssignToSprint(sprint.Id, DateTimeOffset.UtcNow);

        if (assigned.IsFailure)
        {
            return Result.Failure<SprintResponse>(assigned.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await SprintResponseMapper.ToResponseAsync(sprint, dbContext, cancellationToken));
    }

    /// <summary>
    /// Busca el issue dentro del equipo ya autorizado. Que la búsqueda esté acotada por
    /// <c>TeamId</c> es lo que impide sumar a un sprint el issue de otro equipo.
    /// </summary>
    private static async Task<Result<Issue>> FindIssueAsync(
        AppDbContext dbContext,
        Guid teamId,
        string? identifier,
        CancellationToken cancellationToken)
    {
        var normalized = IssueRoute.NormalizeIdentifier(identifier);

        if (normalized.IsFailure)
        {
            return Result.Failure<Issue>(normalized.Error);
        }

        var identifierValue = IssueIdentifier.FromPersistence(normalized.Value);

        var issue = await dbContext.Issues
            .FirstOrDefaultAsync(
                candidate => candidate.TeamId == teamId && candidate.Identifier == identifierValue,
                cancellationToken);

        return issue is null
            ? Result.Failure<Issue>(IssueErrors.NotFound(normalized.Value))
            : Result.Success(issue);
    }
}
