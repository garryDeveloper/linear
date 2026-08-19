using Linear.Domain.Common;

namespace Linear.Domain.Sprints;

public static class SprintErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Sprints.NameRequired", "El nombre del sprint es obligatorio.");

    public static readonly Error NameTooLong = Error.Validation(
        "Sprints.NameTooLong",
        $"El nombre no puede superar los {Sprint.MaxNameLength} caracteres.");

    public static readonly Error GoalTooLong = Error.Validation(
        "Sprints.GoalTooLong",
        $"El objetivo no puede superar los {Sprint.MaxGoalLength} caracteres.");

    public static readonly Error EndDateNotAfterStartDate = Error.Validation(
        "Sprints.EndDateNotAfterStartDate",
        "La fecha de fin tiene que ser posterior a la de inicio.");

    public static readonly Error NotPlanned = Error.Conflict(
        "Sprints.NotPlanned",
        "Solo se puede iniciar un sprint planificado.");

    public static readonly Error NotActive = Error.Conflict(
        "Sprints.NotActive",
        "Solo se puede completar un sprint activo.");

    public static readonly Error Closed = Error.Conflict(
        "Sprints.Closed",
        "El sprint ya está cerrado: no se puede modificar.");

    /// <summary>
    /// La regla central de la task: un equipo tiene a lo sumo un sprint en curso.
    /// </summary>
    public static readonly Error TeamAlreadyHasAnActiveSprint = Error.Conflict(
        "Sprints.TeamAlreadyHasAnActiveSprint",
        "El equipo ya tiene un sprint activo. Completalo o cancelalo antes de iniciar otro.");

    public static readonly Error IssueFromAnotherTeam = Error.Validation(
        "Sprints.IssueFromAnotherTeam",
        "El issue pertenece a otro equipo.");

    public static Error NotFound(Guid sprintId) =>
        Error.NotFound("Sprints.NotFound", $"No existe el sprint '{sprintId}'.");
}
