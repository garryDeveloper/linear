using Linear.Domain.Common;

namespace Linear.Domain.Labels;

public static class LabelErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Labels.NameRequired", "El nombre de la label es obligatorio.");

    public static readonly Error NameTooLong = Error.Validation(
        "Labels.NameTooLong",
        $"El nombre no puede superar los {Label.MaxNameLength} caracteres.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "Labels.DescriptionTooLong",
        $"La descripción no puede superar los {Label.MaxDescriptionLength} caracteres.");

    public static readonly Error NameAlreadyExists = Error.Conflict(
        "Labels.NameAlreadyExists",
        "El equipo ya tiene una label con ese nombre.");

    public static Error NotFound(Guid labelId) =>
        Error.NotFound("Labels.NotFound", $"No existe la label '{labelId}'.");
}
