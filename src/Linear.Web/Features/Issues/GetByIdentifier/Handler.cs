using Linear.Domain.Common;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.GetByIdentifier;

/// <summary>
/// Devuelve un issue completo por su identificador.
/// </summary>
public sealed class GetIssueByIdentifierHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<IssueResponse>> HandleAsync(
        GetIssueByIdentifierRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var resolved = await TeamIssueAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, request.Identifier, trackIssue: false, cancellationToken);

        if (resolved.IsFailure)
        {
            return Result.Failure<IssueResponse>(resolved.Error);
        }

        return Result.Success(
            await IssueResponseMapper.ToResponseAsync(resolved.Value.Issue, dbContext, cancellationToken));
    }
}
