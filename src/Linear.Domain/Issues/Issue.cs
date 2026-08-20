using Linear.Domain.Common;

namespace Linear.Domain.Issues;

/// <summary>
/// Tarea, bug, mejora o historia dentro de un equipo.
/// </summary>
/// <remarks>
/// Es la raíz de su propio agregado, no parte del agregado <c>Team</c>: se consulta y pagina
/// por su cuenta, y referencia al equipo, al responsable y a quien lo creó solo por
/// identificador. <c>SprintId</c> y <c>RoadmapItemId</c> no existen todavía —Sprint y
/// RoadmapItem son de las tasks 007 y 010— así que se suman cuando esas entidades existan,
/// en vez de guardar una referencia a una tabla que no está.
/// </remarks>
public sealed class Issue
{
    public const int MaxTitleLength = 200;
    public const int MinEstimate = 0;
    public const int MaxEstimate = 999;

    private readonly List<IssueLabel> _labels = [];

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Issue()
    {
    }

    private Issue(
        Guid id,
        IssueIdentifier identifier,
        Guid teamId,
        string title,
        string? description,
        Guid createdById,
        DateTimeOffset now)
    {
        Id = id;
        Identifier = identifier;
        TeamId = teamId;
        Title = title.Trim();
        Description = NormalizeDescription(description);
        Status = IssueStatus.Backlog;
        Priority = IssuePriority.None;
        CreatedById = createdById;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Identificador legible, por ejemplo <c>WEB-42</c>. No cambia después de creado: se
    /// arma con la clave del equipo, que tampoco cambia.
    /// </summary>
    public IssueIdentifier Identifier { get; private set; } = null!;

    /// <summary>Equipo dueño del issue. Un issue no cambia de equipo.</summary>
    public Guid TeamId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public IssueStatus Status { get; private set; }

    public IssuePriority Priority { get; private set; }

    public int? Estimate { get; private set; }

    public Guid? AssigneeId { get; private set; }

    /// <summary>
    /// Sprint al que pertenece el issue, si pertenece a alguno.
    /// </summary>
    /// <remarks>
    /// Referencia por identificador, no por objeto: <c>Sprint</c> es raíz de su propio
    /// agregado. Un issue está en un sprint o en ninguno — nunca en dos.
    /// </remarks>
    public Guid? SprintId { get; private set; }

    /// <summary>
    /// Iniciativa del roadmap a la que contribuye el issue, si contribuye a alguna.
    /// </summary>
    /// <remarks>
    /// Referencia por identificador, igual que el sprint. Sprint y roadmap son dos ejes
    /// distintos y compatibles: el sprint dice en qué quincena se trabaja el issue, la
    /// iniciativa dice a qué objetivo de mediano plazo aporta.
    /// </remarks>
    public Guid? RoadmapItemId { get; private set; }

    public Guid CreatedById { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Momento en que el issue pasó a <see cref="IssueStatus.Done"/> por última vez.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public IReadOnlyList<IssueLabel> Labels => _labels.AsReadOnly();

    public static Result<Issue> Create(
        IssueIdentifier identifier,
        Guid teamId,
        string title,
        string? description,
        Guid createdById,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var validation = ValidateTitle(title);

        if (validation.IsFailure)
        {
            return Result.Failure<Issue>(validation.Error);
        }

        return Result.Success(new Issue(
            Guid.CreateVersion7(),
            identifier,
            teamId,
            title,
            description,
            createdById,
            now));
    }

    public Result UpdateContent(string title, string? description, DateTimeOffset now)
    {
        var validation = ValidateTitle(title);

        if (validation.IsFailure)
        {
            return validation;
        }

        Title = title.Trim();
        Description = NormalizeDescription(description);
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Cambia el estado del issue.
    /// </summary>
    /// <remarks>
    /// Entrar a <see cref="IssueStatus.Done"/> marca <see cref="CompletedAt"/>; salir de
    /// <see cref="IssueStatus.Done"/> hacia cualquier otro estado —incluido Canceled— lo
    /// limpia, porque un issue reabierto ya no está completo.
    /// </remarks>
    public void ChangeStatus(IssueStatus status, DateTimeOffset now)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        CompletedAt = status == IssueStatus.Done ? now : null;
        UpdatedAt = now;
    }

    public void ChangePriority(IssuePriority priority, DateTimeOffset now)
    {
        if (Priority == priority)
        {
            return;
        }

        Priority = priority;
        UpdatedAt = now;
    }

    /// <summary>Asigna un responsable, o lo quita si <paramref name="assigneeId"/> es <c>null</c>.</summary>
    public void AssignTo(Guid? assigneeId, DateTimeOffset now)
    {
        if (AssigneeId == assigneeId)
        {
            return;
        }

        AssigneeId = assigneeId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Suma el issue a un sprint. Si ya estaba en otro, lo mueve: un issue pertenece a un
    /// único sprint.
    /// </summary>
    public Result AssignToSprint(Guid sprintId, DateTimeOffset now)
    {
        if (SprintId == sprintId)
        {
            return Result.Failure(IssueErrors.AlreadyInSprint);
        }

        SprintId = sprintId;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RemoveFromSprint(DateTimeOffset now)
    {
        if (SprintId is null)
        {
            return Result.Failure(IssueErrors.NotInASprint);
        }

        SprintId = null;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Asocia el issue a una iniciativa del roadmap. Si ya estaba en otra, lo mueve: un issue
    /// aporta a una única iniciativa.
    /// </summary>
    public Result AssignToRoadmapItem(Guid roadmapItemId, DateTimeOffset now)
    {
        if (RoadmapItemId == roadmapItemId)
        {
            return Result.Failure(IssueErrors.AlreadyInRoadmapItem);
        }

        RoadmapItemId = roadmapItemId;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RemoveFromRoadmapItem(DateTimeOffset now)
    {
        if (RoadmapItemId is null)
        {
            return Result.Failure(IssueErrors.NotInARoadmapItem);
        }

        RoadmapItemId = null;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result ChangeEstimate(int? estimate, DateTimeOffset now)
    {
        if (estimate is < MinEstimate or > MaxEstimate)
        {
            return Result.Failure(IssueErrors.EstimateOutOfRange);
        }

        if (Estimate == estimate)
        {
            return Result.Success();
        }

        Estimate = estimate;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result Archive(DateTimeOffset now)
    {
        if (IsArchived)
        {
            return Result.Failure(IssueErrors.AlreadyArchived);
        }

        ArchivedAt = now;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result AddLabel(Guid labelId, DateTimeOffset now)
    {
        if (HasLabel(labelId))
        {
            return Result.Failure(IssueErrors.LabelAlreadyAdded);
        }

        _labels.Add(new IssueLabel(Id, labelId));
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RemoveLabel(Guid labelId, DateTimeOffset now)
    {
        var label = _labels.FirstOrDefault(l => l.LabelId == labelId);

        if (label is null)
        {
            return Result.Failure(IssueErrors.LabelNotAdded);
        }

        _labels.Remove(label);
        UpdatedAt = now;

        return Result.Success();
    }

    public bool HasLabel(Guid labelId) => _labels.Any(l => l.LabelId == labelId);

    private static Result ValidateTitle(string title) => title switch
    {
        _ when string.IsNullOrWhiteSpace(title) => Result.Failure(IssueErrors.TitleRequired),
        _ when title.Trim().Length > MaxTitleLength => Result.Failure(IssueErrors.TitleTooLong),
        _ => Result.Success()
    };

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
