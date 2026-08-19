using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.AddLabel;

public sealed class AddIssueLabelHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        AddIssueLabelRequest request,
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

        var (team, issue) = resolved.Value;

        var labelExists = await dbContext.Labels
            .AnyAsync(label => label.Id == request.LabelId && label.TeamId == team.Id, cancellationToken);

        if (!labelExists)
        {
            // No se distingue "no existe" de "es de otro equipo": las dos son la misma
            // respuesta desde afuera.
            return Result.Failure<IssueResponse>(IssueErrors.LabelFromAnotherTeam);
        }

        var added = issue.AddLabel(request.LabelId, DateTimeOffset.UtcNow);

        if (added.IsFailure)
        {
            return Result.Failure<IssueResponse>(added.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue, dbContext, cancellationToken));
    }
}
