using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Teams.Contracts;

/// <summary>
/// Arma las respuestas de la feature de equipos.
/// </summary>
/// <remarks>
/// Estos contratos se comparten entre varios slices en lugar de duplicarse: crear, obtener
/// y actualizar un equipo devuelven exactamente la misma representación, y tres copias
/// idénticas se desincronizarían al primer campo nuevo.
/// </remarks>
public static class TeamResponseMapper
{
    public static async Task<TeamResponse> ToResponseAsync(
        Team team,
        Guid currentUserId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(dbContext);

        var userIds = team.Members.Select(member => member.UserId).ToArray();

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var members = team.Members
            .OrderByDescending(member => member.Role)
            .ThenBy(member => member.JoinedAt)
            .Select(member => ToResponse(member, users.GetValueOrDefault(member.UserId)))
            .ToArray();

        return new TeamResponse(
            team.Id,
            team.Key.Value,
            team.Name,
            team.Description,
            (team.RoleOf(currentUserId) ?? TeamRole.Member).ToString(),
            team.CreatedAt,
            members);
    }

    private static TeamMemberResponse ToResponse(TeamMember member, User? user) => new(
        member.UserId,
        // El usuario podría no estar si se eliminó su cuenta entre las dos consultas;
        // el plantel se sigue mostrando en lugar de fallar la operación completa.
        user?.Name ?? "Usuario desconocido",
        user?.Email.Value ?? string.Empty,
        user?.AvatarUrl,
        member.Role.ToString(),
        member.JoinedAt);
}
