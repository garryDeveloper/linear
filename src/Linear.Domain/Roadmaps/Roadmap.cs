using Linear.Domain.Activities;
using Linear.Domain.Common;

namespace Linear.Domain.Roadmaps;

/// <summary>
/// Contenedor de iniciativas de un equipo, para planificar a mediano y largo plazo.
/// </summary>
/// <remarks>
/// Sus iniciativas viven adentro del agregado, tal como lo define el modelo de dominio
/// (.ai/domain-model.md). Acá sí corresponde —y en <c>Comment</c> no correspondió— porque
/// la cantidad está acotada por naturaleza: un roadmap junta unas pocas decenas de
/// iniciativas, no una conversación que crece sin techo. Y sobre todo porque la vista de
/// línea de tiempo las necesita todas a la vez para poder dibujarse: paginarlas sería
/// dibujar media línea de tiempo.
///
/// Los issues asociados no están acá: es el issue el que guarda su <c>RoadmapItemId</c>.
/// </remarks>
public sealed class Roadmap : IHasActivity
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private readonly List<RoadmapItem> _items = [];
    private readonly List<ActivityEvent> _activity = [];

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Roadmap()
    {
    }

    private Roadmap(Guid id, Guid teamId, string name, string? description, DateTimeOffset now)
    {
        Id = id;
        TeamId = teamId;
        Name = name.Trim();
        Description = NormalizeDescription(description);
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Equipo dueño del roadmap. Un roadmap no cambia de equipo.</summary>
    public Guid TeamId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<RoadmapItem> Items => _items.AsReadOnly();

    public static Result<Roadmap> Create(Guid teamId, string name, string? description, DateTimeOffset now)
    {
        var validation = Validate(name, description);

        if (validation.IsFailure)
        {
            return Result.Failure<Roadmap>(validation.Error);
        }

        return Result.Success(new Roadmap(Guid.CreateVersion7(), teamId, name, description, now));
    }

    public Result Update(string name, string? description, DateTimeOffset now)
    {
        var validation = Validate(name, description);

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>Suma una iniciativa. Nace siempre en <see cref="RoadmapItemStatus.Planned"/>.</summary>
    public Result<RoadmapItem> AddItem(
        string name,
        string? description,
        DateOnly startDate,
        DateOnly targetDate,
        DateTimeOffset now)
    {
        var validation = RoadmapItem.Validate(name, description, startDate, targetDate);

        if (validation.IsFailure)
        {
            return Result.Failure<RoadmapItem>(validation.Error);
        }

        var item = new RoadmapItem(Id, name, description, startDate, targetDate, now);

        _items.Add(item);
        UpdatedAt = now;

        Record(ActivityAction.RoadmapItemCreated, item);

        return Result.Success(item);
    }

    public Result UpdateItem(
        Guid itemId,
        string name,
        string? description,
        DateOnly startDate,
        DateOnly targetDate,
        DateTimeOffset now)
    {
        var item = FindItem(itemId);

        if (item is null)
        {
            return Result.Failure(RoadmapErrors.ItemNotFound(itemId));
        }

        var updated = item.Update(name, description, startDate, targetDate, now);

        if (updated.IsFailure)
        {
            return updated;
        }

        UpdatedAt = now;

        Record(ActivityAction.RoadmapItemUpdated, item);

        return Result.Success();
    }

    public Result ChangeItemStatus(Guid itemId, RoadmapItemStatus status, DateTimeOffset now)
    {
        var item = FindItem(itemId);

        if (item is null)
        {
            return Result.Failure(RoadmapErrors.ItemNotFound(itemId));
        }

        item.ChangeStatus(status, now);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RemoveItem(Guid itemId, DateTimeOffset now)
    {
        var item = FindItem(itemId);

        if (item is null)
        {
            return Result.Failure(RoadmapErrors.ItemNotFound(itemId));
        }

        _items.Remove(item);
        UpdatedAt = now;

        return Result.Success();
    }

    public RoadmapItem? FindItem(Guid itemId) => _items.FirstOrDefault(item => item.Id == itemId);

    /// <inheritdoc />
    public IReadOnlyList<ActivityEvent> PendingActivity => _activity.AsReadOnly();

    /// <inheritdoc />
    public void ClearActivity() => _activity.Clear();

    /// <summary>
    /// Registra la actividad de una iniciativa.
    /// </summary>
    /// <remarks>
    /// La entidad afectada es la iniciativa, no el roadmap: es lo que se nombra en el feed.
    /// El evento lo levanta igual la raíz, porque las iniciativas se modifican a través de
    /// ella. Cambiar el estado no lleva acción propia —la task 011 solo define
    /// <c>RoadmapItemUpdated</c>— y por eso va junto con el resto de la edición.
    /// </remarks>
    private void Record(ActivityAction action, RoadmapItem item) =>
        _activity.Add(new ActivityEvent
        {
            EntityType = ActivityEntityType.RoadmapItem,
            EntityId = item.Id,
            Action = action,
            TeamId = TeamId,
            Payload = new Dictionary<string, string?>
            {
                ["name"] = item.Name,
                ["roadmapId"] = Id.ToString(),
                ["roadmapName"] = Name,
                ["status"] = item.Status.ToString()
            }
        });

    private static Result Validate(string name, string? description) =>
        ValidateName(name).Then(() => ValidateDescription(description));

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(RoadmapErrors.NameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(RoadmapErrors.NameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateDescription(string? description) =>
        description?.Trim().Length > MaxDescriptionLength
            ? Result.Failure(RoadmapErrors.DescriptionTooLong)
            : Result.Success();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
