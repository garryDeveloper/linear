using Linear.Domain.Common;

namespace Linear.Domain.Teams;

public static class TeamErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Teams.NameRequired", "El nombre del equipo es obligatorio.");

    public static readonly Error NameTooLong = Error.Validation(
        "Teams.NameTooLong",
        $"El nombre no puede superar los {Team.MaxNameLength} caracteres.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "Teams.DescriptionTooLong",
        $"La descripción no puede superar los {Team.MaxDescriptionLength} caracteres.");

    public static readonly Error KeyAlreadyExists =
        Error.Conflict("Teams.KeyAlreadyExists", "Ya existe un equipo con esa clave.");

    public static readonly Error AlreadyMember =
        Error.Conflict("Teams.AlreadyMember", "El usuario ya pertenece al equipo.");

    public static readonly Error MemberNotFound =
        Error.NotFound("Teams.MemberNotFound", "El usuario no pertenece al equipo.");

    /// <summary>
    /// Un equipo sin Owner quedaría sin nadie que pueda administrarlo ni eliminarlo.
    /// </summary>
    public static readonly Error LastOwner = Error.Conflict(
        "Teams.LastOwner",
        "El equipo debe conservar al menos un Owner.");

    public static readonly Error NotAMember = Error.Forbidden(
        "Teams.NotAMember",
        "No pertenecés a este equipo.");

    public static readonly Error InsufficientRole = Error.Forbidden(
        "Teams.InsufficientRole",
        "Tu rol en el equipo no permite esta operación.");

    /// <summary>
    /// Si un Admin pudiera repartir el rol Owner, podría concedérselo a sí mismo a través
    /// de un tercero y escalar sus permisos.
    /// </summary>
    public static readonly Error OnlyOwnersManageOwners = Error.Forbidden(
        "Teams.OnlyOwnersManageOwners",
        "Solo un Owner puede asignar o quitar el rol Owner.");

    public static Error NotFound(Guid teamId) =>
        Error.NotFound("Teams.NotFound", $"No existe el equipo '{teamId}'.");

    public static Error NotFoundByKey(string key) =>
        Error.NotFound("Teams.NotFound", $"No existe el equipo '{key}'.");
}
