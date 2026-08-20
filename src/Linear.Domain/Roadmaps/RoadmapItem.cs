using Linear.Domain.Common;

namespace Linear.Domain.Roadmaps;

/// <summary>
/// Iniciativa dentro de un roadmap: un tramo de trabajo con fechas y estado.
/// </summary>
/// <remarks>
/// Vive dentro del agregado <see cref="Roadmap"/> y se crea y modifica a través de él, igual
/// que <c>TeamMember</c> respecto de <c>Team</c>.
///
/// Los issues que la iniciativa agrupa no viven acá: es el issue el que guarda su
/// <c>RoadmapItemId</c>, porque <c>Issue</c> es raíz de su propio agregado y su cantidad no
/// tiene techo.
/// </remarks>
public sealed class RoadmapItem
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private RoadmapItem()
    {
    }

    internal RoadmapItem(
        Guid roadmapId,
        string name,
        string? description,
        DateOnly startDate,
        DateOnly targetDate,
        DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        RoadmapId = roadmapId;
        Name = name.Trim();
        Description = NormalizeDescription(description);
        StartDate = startDate;
        TargetDate = targetDate;
        Status = RoadmapItemStatus.Planned;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid RoadmapId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public RoadmapItemStatus Status { get; private set; }

    /// <summary>
    /// Fechas de calendario, no instantes: una iniciativa dura semanas o meses, y guardarla
    /// como <c>DateTimeOffset</c> obligaría a inventar una hora. Es el mismo criterio que en
    /// <c>Sprint</c>.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Fecha a la que se apunta a terminarla. Es un objetivo, no un vencimiento.</summary>
    public DateOnly TargetDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal Result Update(
        string name,
        string? description,
        DateOnly startDate,
        DateOnly targetDate,
        DateTimeOffset now)
    {
        var validation = Validate(name, description, startDate, targetDate);

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        StartDate = startDate;
        TargetDate = targetDate;
        UpdatedAt = now;

        return Result.Success();
    }

    internal void ChangeStatus(RoadmapItemStatus status, DateTimeOffset now)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAt = now;
    }

    internal static Result Validate(
        string name,
        string? description,
        DateOnly startDate,
        DateOnly targetDate) =>
        ValidateName(name)
            .Then(() => ValidateDescription(description))
            .Then(() => ValidateDates(startDate, targetDate));

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(RoadmapErrors.ItemNameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(RoadmapErrors.ItemNameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateDescription(string? description) =>
        description?.Trim().Length > MaxDescriptionLength
            ? Result.Failure(RoadmapErrors.ItemDescriptionTooLong)
            : Result.Success();

    private static Result ValidateDates(DateOnly startDate, DateOnly targetDate) =>
        targetDate > startDate
            ? Result.Success()
            : Result.Failure(RoadmapErrors.TargetDateNotAfterStartDate);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
