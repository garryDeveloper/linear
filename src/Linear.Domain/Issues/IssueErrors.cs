using Linear.Domain.Common;

namespace Linear.Domain.Issues;

public static class IssueErrors
{
    public static readonly Error TitleRequired =
        Error.Validation("Issues.TitleRequired", "El título del issue es obligatorio.");

    public static readonly Error TitleTooLong = Error.Validation(
        "Issues.TitleTooLong",
        $"El título no puede superar los {Issue.MaxTitleLength} caracteres.");

    public static readonly Error EstimateOutOfRange = Error.Validation(
        "Issues.EstimateOutOfRange",
        $"El estimate debe estar entre 0 y {Issue.MaxEstimate}.");

    public static readonly Error AlreadyArchived =
        Error.Conflict("Issues.AlreadyArchived", "El issue ya está archivado.");

    /// <summary>
    /// El issue cambió entre que quien edita lo cargó y que intentó guardar.
    /// </summary>
    /// <remarks>
    /// Es la estrategia de conflictos de la V1: el que llega segundo no pisa al primero en
    /// silencio, se entera. Deliberadamente no se intenta fusionar los dos cambios —eso es
    /// edición colaborativa, que la task 014 excluye—: se le devuelve el control a la
    /// persona, que es la única que sabe si su versión sigue teniendo sentido.
    /// </remarks>
    public static readonly Error ModifiedByAnother = Error.Conflict(
        "Issues.ModifiedByAnother",
        "Alguien más modificó este issue mientras lo editabas. Revisá los cambios antes de guardar.");

    public static readonly Error Archived = Error.Conflict(
        "Issues.Archived",
        "El issue está archivado. Desarchivalo antes de modificarlo.");

    public static readonly Error LabelAlreadyAdded =
        Error.Conflict("Issues.LabelAlreadyAdded", "El issue ya tiene esa label.");

    public static readonly Error LabelNotAdded =
        Error.NotFound("Issues.LabelNotAdded", "El issue no tiene esa label.");

    public static readonly Error LabelFromAnotherTeam = Error.Validation(
        "Issues.LabelFromAnotherTeam",
        "La label pertenece a otro equipo.");

    public static readonly Error AssigneeNotAMember = Error.Validation(
        "Issues.AssigneeNotAMember",
        "El responsable tiene que ser miembro del equipo.");

    public static readonly Error AlreadyInSprint =
        Error.Conflict("Issues.AlreadyInSprint", "El issue ya está en ese sprint.");

    public static readonly Error NotInASprint =
        Error.NotFound("Issues.NotInASprint", "El issue no está en ese sprint.");

    public static readonly Error AlreadyInRoadmapItem = Error.Conflict(
        "Issues.AlreadyInRoadmapItem",
        "El issue ya está asociado a esa iniciativa.");

    public static readonly Error NotInARoadmapItem = Error.NotFound(
        "Issues.NotInARoadmapItem",
        "El issue no está asociado a esa iniciativa.");

    public static Error NotFound(string identifier) =>
        Error.NotFound("Issues.NotFound", $"No existe el issue '{identifier}'.");
}
