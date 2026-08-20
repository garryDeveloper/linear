using Linear.Domain.Common;
using Linear.Domain.Roadmaps;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.Create;

/// <summary>
/// Crea un roadmap vacío dentro de un equipo.
/// </summary>
public sealed class CreateRoadmapHandler(
    ITeamAccess teamAccess,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<Result<RoadmapResponse>> HandleAsync(
        CreateRoadmapRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var team = await TeamSectionAccess.RequireMemberAsync(
            teamAccess, dbContext, request.Key, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(team.Error);
        }

        var roadmap = Roadmap.Create(
            team.Value.Id, request.Name, request.Description, DateTimeOffset.UtcNow);

        if (roadmap.IsFailure)
        {
            return Result.Failure<RoadmapResponse>(roadmap.Error);
        }

        dbContext.Roadmaps.Add(roadmap.Value);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await RoadmapResponseMapper.ToResponseAsync(roadmap.Value, dbContext, cancellationToken));
    }
}
