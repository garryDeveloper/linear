using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Contracts;

/// <summary>
/// Resuelve el equipo y el issue que identifica la ruta, con el rol mínimo que la
/// operación necesita.
/// </summary>
/// <remarks>
/// Primero resuelve el equipo —lo que ya aísla por pertenencia y responde 404 sin
/// distinguir "no existe" de "no tenés acceso"— y recién después busca el issue acotado a
/// ese equipo ya autorizado. Pedir el issue de otro equipo por la ruta equivocada nunca lo
/// encuentra: la búsqueda está acotada por <c>TeamId</c>, no solo por identificador.
/// </remarks>
public static class TeamIssueAccess
{
    public static async Task<Result<(Team Team, Issue Issue)>> RequireMemberAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        bool trackIssue,
        CancellationToken cancellationToken) =>
        await ResolveAsync(
            teamAccess,
            dbContext,
            teamKey,
            identifier,
            TeamSectionAccess.RequireMemberAsync,
            trackIssue,
            cancellationToken);

    public static async Task<Result<(Team Team, Issue Issue)>> RequireAdminAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        bool trackIssue,
        CancellationToken cancellationToken) =>
        await ResolveAsync(
            teamAccess,
            dbContext,
            teamKey,
            identifier,
            TeamSectionAccess.RequireAdminAsync,
            trackIssue,
            cancellationToken);

    private static async Task<Result<(Team Team, Issue Issue)>> ResolveAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        string? identifier,
        Func<ITeamAccess, AppDbContext, string?, CancellationToken, Task<Result<Team>>> resolveTeam,
        bool trackIssue,
        CancellationToken cancellationToken)
    {
        var normalizedIdentifier = IssueRoute.NormalizeIdentifier(identifier);

        if (normalizedIdentifier.IsFailure)
        {
            return Result.Failure<(Team, Issue)>(normalizedIdentifier.Error);
        }

        var team = await resolveTeam(teamAccess, dbContext, teamKey, cancellationToken);

        if (team.IsFailure)
        {
            return Result.Failure<(Team, Issue)>(team.Error);
        }

        var identifierValue = IssueIdentifier.FromPersistence(normalizedIdentifier.Value);

        // Se incluyen las labels porque esta resolución siempre trae un único issue: acá no
        // hay paginación de por medio, así que no aplica el problema clásico de Include
        // duplicando filas al combinarse con Skip/Take (el listado no pasa por acá).
        var query = dbContext.Issues
            .Include(candidate => candidate.Labels)
            .Where(candidate =>
                candidate.TeamId == team.Value.Id &&
                candidate.Identifier == identifierValue);

        var issue = await (trackIssue ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        return issue is null
            ? Result.Failure<(Team, Issue)>(IssueErrors.NotFound(normalizedIdentifier.Value))
            : Result.Success((team.Value, issue));
    }
}
