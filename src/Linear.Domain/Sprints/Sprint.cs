using Linear.Domain.Activities;
using Linear.Domain.Common;

namespace Linear.Domain.Sprints;

/// <summary>
/// Período de trabajo acotado de un equipo.
/// </summary>
/// <remarks>
/// Es raíz de su propio agregado y referencia al equipo solo por identificador, igual que
/// <c>Issue</c> o <c>Label</c>. Los issues que contiene tampoco viven acá adentro: es el
/// issue el que guarda su <c>SprintId</c>, así que mover un issue de sprint no obliga a
/// cargar ninguno de los dos sprints enteros.
///
/// La regla de "un solo sprint activo por equipo" no se puede sostener desde esta clase
/// —una entidad no ve a sus hermanas—, así que vive donde sí es infalsificable: un índice
/// único parcial en la base. Ver <c>SprintConfiguration</c>.
/// </remarks>
public sealed class Sprint : IHasActivity
{
    public const int MaxNameLength = 100;
    public const int MaxGoalLength = 500;

    private readonly List<ActivityEvent> _activity = [];

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Sprint()
    {
    }

    private Sprint(
        Guid id,
        Guid teamId,
        string name,
        string? goal,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset now)
    {
        Id = id;
        TeamId = teamId;
        Name = name.Trim();
        Goal = NormalizeGoal(goal);
        StartDate = startDate;
        EndDate = endDate;
        Status = SprintStatus.Planned;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Equipo dueño del sprint. Un sprint no cambia de equipo.</summary>
    public Guid TeamId { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Objetivo del sprint, en texto libre.</summary>
    public string? Goal { get; private set; }

    /// <summary>
    /// Fechas de calendario y no instantes: un sprint dura días completos, y guardarlo como
    /// <c>DateTimeOffset</c> obligaría a inventar una hora y a decidir en qué huso termina.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <inheritdoc cref="StartDate"/>
    public DateOnly EndDate { get; private set; }

    public SprintStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Momento en que se completó. Un sprint cancelado nunca lo tiene.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsActive => Status == SprintStatus.Active;

    /// <summary>Indica si el sprint ya terminó su ciclo y no admite más cambios.</summary>
    public bool IsClosed => Status is SprintStatus.Completed or SprintStatus.Canceled;

    public static Result<Sprint> Create(
        Guid teamId,
        string name,
        string? goal,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset now)
    {
        var validation = Validate(name, goal, startDate, endDate);

        if (validation.IsFailure)
        {
            return Result.Failure<Sprint>(validation.Error);
        }

        return Result.Success(new Sprint(Guid.CreateVersion7(), teamId, name, goal, startDate, endDate, now));
    }

    public Result Update(string name, string? goal, DateOnly startDate, DateOnly endDate, DateTimeOffset now)
    {
        if (IsClosed)
        {
            return Result.Failure(SprintErrors.Closed);
        }

        var validation = Validate(name, goal, startDate, endDate);

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Goal = NormalizeGoal(goal);
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Pone el sprint en curso.
    /// </summary>
    /// <remarks>
    /// Que el equipo no tenga ya otro sprint activo se verifica afuera: es una regla entre
    /// hermanos, no dentro de esta entidad.
    /// </remarks>
    public Result Start(DateTimeOffset now)
    {
        if (Status != SprintStatus.Planned)
        {
            return Result.Failure(SprintErrors.NotPlanned);
        }

        Status = SprintStatus.Active;
        UpdatedAt = now;

        Record(ActivityAction.SprintStarted);

        return Result.Success();
    }

    public Result Complete(DateTimeOffset now)
    {
        if (Status != SprintStatus.Active)
        {
            return Result.Failure(SprintErrors.NotActive);
        }

        Status = SprintStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;

        Record(ActivityAction.SprintCompleted);

        return Result.Success();
    }

    /// <summary>
    /// Cancela el sprint, esté planificado o en curso.
    /// </summary>
    /// <remarks>
    /// No marca <see cref="CompletedAt"/>: un sprint cancelado no se completó, y confundir
    /// ambas cosas ensuciaría cualquier métrica que cuente sprints terminados.
    /// </remarks>
    public Result Cancel(DateTimeOffset now)
    {
        if (IsClosed)
        {
            return Result.Failure(SprintErrors.Closed);
        }

        Status = SprintStatus.Canceled;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <inheritdoc />
    public IReadOnlyList<ActivityEvent> PendingActivity => _activity.AsReadOnly();

    /// <inheritdoc />
    public void ClearActivity() => _activity.Clear();

    /// <summary>
    /// Registra iniciar y completar el sprint.
    /// </summary>
    /// <remarks>
    /// Solo esas dos: son las que define la task 011. Crear, editar y cancelar no tienen
    /// acción en el vocabulario, y como el historial es append-only, inventarlas sería
    /// agregar términos que después no se pueden corregir.
    /// </remarks>
    private void Record(ActivityAction action) =>
        _activity.Add(new ActivityEvent
        {
            EntityType = ActivityEntityType.Sprint,
            EntityId = Id,
            Action = action,
            TeamId = TeamId,
            Payload = new Dictionary<string, string?> { ["name"] = Name }
        });

    private static Result Validate(string name, string? goal, DateOnly startDate, DateOnly endDate) =>
        ValidateName(name)
            .Then(() => ValidateGoal(goal))
            .Then(() => ValidateDates(startDate, endDate));

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(SprintErrors.NameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(SprintErrors.NameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateGoal(string? goal) =>
        goal?.Trim().Length > MaxGoalLength
            ? Result.Failure(SprintErrors.GoalTooLong)
            : Result.Success();

    private static Result ValidateDates(DateOnly startDate, DateOnly endDate) =>
        endDate > startDate
            ? Result.Success()
            : Result.Failure(SprintErrors.EndDateNotAfterStartDate);

    private static string? NormalizeGoal(string? goal) =>
        string.IsNullOrWhiteSpace(goal) ? null : goal.Trim();
}
