using Linear.Domain.Common;

namespace Linear.Web.Features.Teams.AddMember;

/// <summary>
/// Errores propios de sumar miembros, que dependen de datos fuera del agregado Team.
/// </summary>
public static class TeamMemberErrors
{
    public static readonly Error UserInactive = Error.Validation(
        "Teams.UserInactive",
        "La cuenta de ese usuario está desactivada.");

    public static Error UserNotFound(string email) => Error.NotFound(
        "Teams.UserNotFound",
        $"No existe un usuario con el email '{email}'. Tiene que tener una cuenta creada.");
}
