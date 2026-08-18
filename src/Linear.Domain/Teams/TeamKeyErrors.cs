using Linear.Domain.Common;

namespace Linear.Domain.Teams;

public static class TeamKeyErrors
{
    public static readonly Error Empty =
        Error.Validation("TeamKey.Empty", "La clave del equipo es obligatoria.");

    public static readonly Error InvalidLength = Error.Validation(
        "TeamKey.InvalidLength",
        $"La clave debe tener entre {TeamKey.MinLength} y {TeamKey.MaxLength} caracteres.");

    public static readonly Error InvalidFormat = Error.Validation(
        "TeamKey.InvalidFormat",
        "La clave debe empezar con una letra y contener solo letras y números.");
}
