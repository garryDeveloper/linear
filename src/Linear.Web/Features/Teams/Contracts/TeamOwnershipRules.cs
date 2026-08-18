using Linear.Domain.Common;
using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.Contracts;

/// <summary>
/// Restricción que separa a los Admin de los Owner en la gestión del plantel.
/// </summary>
/// <remarks>
/// El agregado <see cref="Team"/> garantiza que nunca falte un Owner, pero no sabe quién
/// pide el cambio. Esta regla es de autorización, no de consistencia, y por eso vive acá:
/// si un Admin pudiera repartir el rol Owner, podría concedérselo a sí mismo mediante un
/// tercero y quedarse con el control del equipo.
/// </remarks>
public static class TeamOwnershipRules
{
    public static Result EnsureOwnerPrivilege(
        Team team,
        Guid actingUserId,
        TeamRole? currentTargetRole,
        TeamRole? newTargetRole)
    {
        ArgumentNullException.ThrowIfNull(team);

        var involvesAnOwner = currentTargetRole == TeamRole.Owner || newTargetRole == TeamRole.Owner;

        if (!involvesAnOwner)
        {
            return Result.Success();
        }

        return team.RoleOf(actingUserId) == TeamRole.Owner
            ? Result.Success()
            : Result.Failure(TeamErrors.OnlyOwnersManageOwners);
    }
}
