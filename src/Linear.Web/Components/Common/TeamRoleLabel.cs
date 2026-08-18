using Linear.Domain.Teams;

namespace Linear.Web.Components.Common;

/// <summary>
/// Nombres de los roles de equipo tal como se muestran en la interfaz.
/// </summary>
public static class TeamRoleLabel
{
    public static string For(string role) =>
        Enum.TryParse<TeamRole>(role, out var parsed) ? For(parsed) : role;

    public static string For(TeamRole role) => role switch
    {
        TeamRole.Owner => "Owner",
        TeamRole.Admin => "Admin",
        TeamRole.Member => "Miembro",
        _ => role.ToString()
    };
}
