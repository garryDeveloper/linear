using Linear.Domain.Common;

namespace Linear.Domain.Roadmaps;

public static class RoadmapErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Roadmaps.NameRequired", "El nombre del roadmap es obligatorio.");

    public static readonly Error NameTooLong = Error.Validation(
        "Roadmaps.NameTooLong",
        $"El nombre no puede superar los {Roadmap.MaxNameLength} caracteres.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "Roadmaps.DescriptionTooLong",
        $"La descripción no puede superar los {Roadmap.MaxDescriptionLength} caracteres.");

    public static readonly Error ItemNameRequired =
        Error.Validation("Roadmaps.ItemNameRequired", "El nombre de la iniciativa es obligatorio.");

    public static readonly Error ItemNameTooLong = Error.Validation(
        "Roadmaps.ItemNameTooLong",
        $"El nombre no puede superar los {RoadmapItem.MaxNameLength} caracteres.");

    public static readonly Error ItemDescriptionTooLong = Error.Validation(
        "Roadmaps.ItemDescriptionTooLong",
        $"La descripción no puede superar los {RoadmapItem.MaxDescriptionLength} caracteres.");

    public static readonly Error TargetDateNotAfterStartDate = Error.Validation(
        "Roadmaps.TargetDateNotAfterStartDate",
        "La fecha objetivo tiene que ser posterior a la de inicio.");

    public static Error NotFound(Guid roadmapId) =>
        Error.NotFound("Roadmaps.NotFound", $"No existe el roadmap '{roadmapId}'.");

    public static Error ItemNotFound(Guid itemId) =>
        Error.NotFound("Roadmaps.ItemNotFound", $"No existe la iniciativa '{itemId}'.");
}
