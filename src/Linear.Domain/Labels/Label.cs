using Linear.Domain.Common;

namespace Linear.Domain.Labels;

/// <summary>
/// Etiqueta con la que se categorizan los issues de un equipo.
/// </summary>
/// <remarks>
/// Pertenece a un único equipo y no se comparte entre equipos: las labels globales están
/// fuera del alcance de la V1.
/// Se modela como raíz propia y no dentro del agregado <c>Team</c> porque se consulta y se
/// pagina por su cuenta, y porque cargar el equipo entero —con todos sus miembros— para
/// renombrar una label sería desproporcionado.
/// </remarks>
public sealed class Label
{
    public const int MaxNameLength = 50;
    public const int MaxDescriptionLength = 500;

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private Label()
    {
    }

    private Label(
        Guid id,
        Guid teamId,
        string name,
        string? description,
        LabelColor color,
        DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        Color = color;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        ApplyName(name);
        Description = NormalizeDescription(description);
    }

    public Guid Id { get; private set; }

    public Guid TeamId { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>
    /// Nombre en mayúsculas, sobre el que se apoya el índice único por equipo.
    /// </summary>
    /// <remarks>
    /// Permite que la unicidad no distinga mayúsculas de minúsculas —tener "bug" y "Bug"
    /// en el mismo equipo solo genera confusión— sin perder cómo lo escribió el usuario.
    /// </remarks>
    public string NormalizedName { get; private set; } = null!;

    public string? Description { get; private set; }

    public LabelColor Color { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Label> Create(
        Guid teamId,
        string name,
        string? description,
        LabelColor color,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(color);

        var validation = ValidateName(name).Then(() => ValidateDescription(description));

        if (validation.IsFailure)
        {
            return Result.Failure<Label>(validation.Error);
        }

        return Result.Success(new Label(Guid.CreateVersion7(), teamId, name, description, color, now));
    }

    public Result Update(string name, string? description, LabelColor color, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(color);

        var validation = ValidateName(name).Then(() => ValidateDescription(description));

        if (validation.IsFailure)
        {
            return validation;
        }

        ApplyName(name);
        Description = NormalizeDescription(description);
        Color = color;
        UpdatedAt = now;

        return Result.Success();
    }

    private void ApplyName(string name)
    {
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
    }

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(LabelErrors.NameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(LabelErrors.NameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateDescription(string? description) =>
        description?.Trim().Length > MaxDescriptionLength
            ? Result.Failure(LabelErrors.DescriptionTooLong)
            : Result.Success();

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
