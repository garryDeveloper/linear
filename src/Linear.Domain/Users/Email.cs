using System.Net.Mail;

using Linear.Domain.Common;

namespace Linear.Domain.Users;

/// <summary>
/// Dirección de correo válida y normalizada.
/// </summary>
/// <remarks>
/// Existe como value object para que la normalización ocurra en un solo lugar: el email
/// identifica al usuario en el login y es único en la base, así que dos representaciones
/// distintas del mismo email ("Ana@Linear.dev" y "ana@linear.dev") serían dos usuarios.
/// </remarks>
public sealed record Email
{
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Valida y normaliza una dirección de correo.
    /// </summary>
    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(EmailErrors.Empty);
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return Result.Failure<Email>(EmailErrors.TooLong);
        }

        return IsWellFormed(normalized)
            ? Result.Success(new Email(normalized))
            : Result.Failure<Email>(EmailErrors.InvalidFormat);
    }

    /// <summary>
    /// Reconstruye un email ya validado, sin volver a validarlo.
    /// </summary>
    /// <remarks>
    /// Solo para materializar desde la base de datos: lo que está persistido pasó por
    /// <see cref="Create"/> en su momento y volver a validarlo haría que un cambio en las
    /// reglas rompiera la lectura de datos existentes.
    /// </remarks>
    public static Email FromPersistence(string value) => new(value);

    public override string ToString() => Value;

    private static bool IsWellFormed(string value)
    {
        // MailAddress acepta formas como "Nombre <a@b.com>" que no queremos almacenar,
        // así que además se exige que la dirección coincida exactamente con la entrada.
        if (value.Any(char.IsWhiteSpace) || value.Count(character => character == '@') != 1)
        {
            return false;
        }

        return MailAddress.TryCreate(value, out var address) &&
               string.Equals(address.Address, value, StringComparison.Ordinal);
    }
}
