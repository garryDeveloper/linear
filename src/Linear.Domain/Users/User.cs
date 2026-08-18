using Linear.Domain.Common;

namespace Linear.Domain.Users;

/// <summary>
/// Persona que utiliza el sistema.
/// </summary>
/// <remarks>
/// Guarda el hash de la contraseña, no la contraseña. El dominio no sabe cómo se calcula
/// ese hash —eso es responsabilidad de la infraestructura— pero sí exige que exista.
/// </remarks>
public sealed class User
{
    public const int MaxNameLength = 100;
    public const int MaxAvatarUrlLength = 2048;

    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private User()
    {
    }

    private User(
        Guid id,
        Email email,
        string name,
        UserRole role,
        string passwordHash,
        string? avatarUrl,
        DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        Name = name;
        Role = role;
        PasswordHash = passwordHash;
        AvatarUrl = avatarUrl;
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Email Email { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? AvatarUrl { get; private set; }

    public UserRole Role { get; private set; }

    /// <summary>Hash de la contraseña. Nunca la contraseña en claro.</summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>Una cuenta desactivada no puede iniciar sesión ni sostener una sesión abierta.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<User> Create(
        Email email,
        string name,
        UserRole role,
        string passwordHash,
        DateTimeOffset now,
        string? avatarUrl = null)
    {
        ArgumentNullException.ThrowIfNull(email);

        var validation = ValidateName(name)
            .Then(() => ValidateAvatarUrl(avatarUrl))
            .Then(() => ValidatePasswordHash(passwordHash));

        if (validation.IsFailure)
        {
            return Result.Failure<User>(validation.Error);
        }

        return Result.Success(new User(
            Guid.CreateVersion7(),
            email,
            name.Trim(),
            role,
            passwordHash,
            NormalizeAvatarUrl(avatarUrl),
            now));
    }

    public Result Rename(string name, DateTimeOffset now)
    {
        var validation = ValidateName(name);

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        UpdatedAt = now;

        return Result.Success();
    }

    public Result ChangeAvatarUrl(string? avatarUrl, DateTimeOffset now)
    {
        var validation = ValidateAvatarUrl(avatarUrl);

        if (validation.IsFailure)
        {
            return validation;
        }

        AvatarUrl = NormalizeAvatarUrl(avatarUrl);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result ChangePasswordHash(string passwordHash, DateTimeOffset now)
    {
        var validation = ValidatePasswordHash(passwordHash);

        if (validation.IsFailure)
        {
            return validation;
        }

        PasswordHash = passwordHash;
        UpdatedAt = now;

        return Result.Success();
    }

    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        if (Role == role)
        {
            return;
        }

        Role = role;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = now;
    }

    private static Result ValidateName(string name) => name switch
    {
        _ when string.IsNullOrWhiteSpace(name) => Result.Failure(UserErrors.NameRequired),
        _ when name.Trim().Length > MaxNameLength => Result.Failure(UserErrors.NameTooLong),
        _ => Result.Success()
    };

    private static Result ValidateAvatarUrl(string? avatarUrl) =>
        avatarUrl?.Trim().Length > MaxAvatarUrlLength
            ? Result.Failure(UserErrors.AvatarUrlTooLong)
            : Result.Success();

    private static Result ValidatePasswordHash(string passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? Result.Failure(UserErrors.PasswordHashRequired)
            : Result.Success();

    private static string? NormalizeAvatarUrl(string? avatarUrl) =>
        string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
}
