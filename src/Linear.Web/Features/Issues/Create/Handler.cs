using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Issues;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Create;

/// <summary>
/// Crea un issue dentro de un equipo.
/// </summary>
public sealed class CreateIssueHandler(
    ITeamAccess teamAccess,
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        CreateIssueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<IssueResponse>(team.Error);
        }

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<IssueResponse>(userId.Error);
        }

        if (request.AssigneeId is { } assigneeId && !team.Value.HasMember(assigneeId))
        {
            return Result.Failure<IssueResponse>(IssueErrors.AssigneeNotAMember);
        }

        var labelIds = request.LabelIds.Distinct().ToArray();

        var labelsValidation = await ValidateLabelsBelongToTeamAsync(
            dbContext, team.Value.Id, labelIds, cancellationToken);

        if (labelsValidation.IsFailure)
        {
            return Result.Failure<IssueResponse>(labelsValidation.Error);
        }

        var now = DateTimeOffset.UtcNow;
        var number = await IssueNumberSequence.NextAsync(dbContext, team.Value.Id, cancellationToken);
        var identifier = IssueIdentifier.Create(team.Value.Key, number);

        var issue = Issue.Create(identifier, team.Value.Id, request.Title, request.Description, userId.Value, now);

        if (issue.IsFailure)
        {
            return Result.Failure<IssueResponse>(issue.Error);
        }

        if (request.AssigneeId is { } assignee)
        {
            issue.Value.AssignTo(assignee, now);
        }

        foreach (var labelId in labelIds)
        {
            issue.Value.AddLabel(labelId, now);
        }

        dbContext.Issues.Add(issue.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await IssueResponseMapper.ToResponseAsync(issue.Value, dbContext, cancellationToken));
    }

    private static async Task<Result> ValidateLabelsBelongToTeamAsync(
        AppDbContext dbContext,
        Guid teamId,
        IReadOnlyCollection<Guid> labelIds,
        CancellationToken cancellationToken)
    {
        if (labelIds.Count == 0)
        {
            return Result.Success();
        }

        var validCount = await dbContext.Labels
            .Where(label => labelIds.Contains(label.Id) && label.TeamId == teamId)
            .CountAsync(cancellationToken);

        // No se distingue "no existe" de "es de otro equipo": las dos son la misma
        // respuesta desde afuera, y distinguirlas dejaría averiguar qué labels hay en un
        // equipo ajeno.
        return validCount == labelIds.Count
            ? Result.Success()
            : Result.Failure(IssueErrors.LabelFromAnotherTeam);
    }
}
