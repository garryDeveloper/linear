using Linear.Domain.Common;

namespace Linear.Domain.Teams;

/// <summary>
/// Unidad organizativa principal: todo issue, label y sprint pertenece a un equipo.
/// </summary>
/// <remarks>
/// Es la raíz del agregado que incluye a sus miembros. Las reglas que dependen del conjunto
/// —que no se repita un usuario, que nunca falte un Owner— solo pueden garantizarse desde
/// acá, así que <see cref="TeamMember"/> no se manipula por fuera.
/// </remarks>
public sealed class Team
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1000;

    private readonly List<TeamMember> _members = [];

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Team()
    {
    }

    private Team(Guid id, string name, TeamKey key, string? description, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Key = key;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>
    /// Clave del equipo. No cambia después de creado.
    /// </summary>
    /// <remarks>
    /// Los identificadores de los issues la incorporan (<c>WEB-12</c>). Permitir cambiarla
    /// obligaría a reescribir cada identificador ya comunicado en enlaces, commits y
    /// conversaciones, o a dejarlos apuntando a una clave que ya no existe.
    /// </remarks>
    public TeamKey Key { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Último número de issue asignado en este equipo.
    /// </summary>
    /// <remarks>
    /// Ningún método de <see cref="Team"/> lo modifica: es <c>IssueNumberSequence</c>, en
    /// infraestructura, quien lo avanza con una sola sentencia SQL atómica
    /// (<c>UPDATE ... RETURNING</c>), para que dos issues creados a la vez nunca reciban el
    /// mismo número. Vive acá y no en <c>Issue</c> porque el número se reserva antes de que
    /// el issue exista.
    /// </remarks>
    public int LastIssueNumber { get; private set; }

    public IReadOnlyList<TeamMember> Members => _members.AsReadOnly();

    /// <summary>
    /// Crea un equipo junto a su primer Owner.
    /// </summary>
    /// <remarks>
    /// El Owner se asigna en el mismo acto que la creación: un equipo no debe existir en
    /// ningún momento sin alguien que pueda administrarlo.
    /// </remarks>
    public static Result<Team> Create(
        string name,
        TeamKey key,
        string? description,
        Guid ownerUserId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(key);

        var validation = ValidateName(name).Then(() => ValidateDescription(description));

        if (validation.IsFailure)
        {
            return Result.Failure<Team>(validation.Error);
        }

        var team = new Team(Guid.CreateVersion7(), name.Trim(), key, NormalizeDescription(description), now);

        team._members.Add(new TeamMember(team.Id, ownerUserId, TeamRole.Owner, now));

        return Result.Success(team);
    }

    public Result Update(string name, string? description, DateTimeOffset now)
    {
        var validation = ValidateName(name).Then(() => ValidateDescription(description));

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Description = NormalizeDescription(description);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result AddMember(Guid userId, TeamRole role, DateTimeOffset now)
    {
        if (HasMember(userId))
        {
            return Result.Failure(TeamErrors.AlreadyMember);
        }

        _members.Add(new TeamMember(Id, userId, role, now));
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RemoveMember(Guid userId, DateTimeOffset now)
    {
        var member = FindMember(userId);

        if (member is null)
        {
            return Result.Failure(TeamErrors.MemberNotFound);
        }

        if (member.Role == TeamRole.Owner && OwnerCount == 1)
        {
            return Result.Failure(TeamErrors.LastOwner);
        }

        _members.Remove(member);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result ChangeMemberRole(Guid userId, TeamRole role, DateTimeOffset now)
    {
        var member = FindMember(userId);

        if (member is null)
        {
            return Result.Failure(TeamErrors.MemberNotFound);
        }

        if (member.Role == role)
        {
            return Result.Success();
        }

        if (member.Role == TeamRole.Owner && OwnerCount == 1)
        {
            return Result.Failure(TeamErrors.LastOwner);
        }

        member.ChangeRole(role);
        UpdatedAt = now;

        return Result.Success();
    }

    public bool HasMember(Guid userId) => FindMember(userId) is not null;

    /// <summary>Rol del usuario en el equipo, o <c>null</c> si no pertenece.</summary>
    public TeamRole? RoleOf(Guid userId) => FindMember(userId)?.Role;

    private int OwnerCount => _members.Count(member => member.Role == TeamRole.Owner);

    private TeamMember? FindMember(Guid userId) =>
        _members.FirstOrDefault(member => member.UserId == userId);

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(TeamErrors.NameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(TeamErrors.NameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateDescription(string? description) =>
        description?.Trim().Length > MaxDescriptionLength
            ? Result.Failure(TeamErrors.DescriptionTooLong)
            : Result.Success();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
