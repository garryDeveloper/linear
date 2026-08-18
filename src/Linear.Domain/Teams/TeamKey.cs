using Linear.Domain.Common;

namespace Linear.Domain.Teams;

/// <summary>
/// Identificador corto de un equipo: <c>WEB</c>, <c>CORE</c>, <c>MOBILE</c>.
/// </summary>
/// <remarks>
/// Es la primera mitad del identificador de cada issue (<c>WEB-12</c>), así que se
/// normaliza a mayúsculas y se restringe a caracteres que no necesiten escaparse en una
/// URL ni resulten ambiguos al leerlos.
/// </remarks>
public sealed record TeamKey
{
    public const int MinLength = 2;
    public const int MaxLength = 8;

    private TeamKey(string value) => Value = value;

    public string Value { get; }

    public static Result<TeamKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TeamKey>(TeamKeyErrors.Empty);
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length is < MinLength or > MaxLength)
        {
            return Result.Failure<TeamKey>(TeamKeyErrors.InvalidLength);
        }

        return IsWellFormed(normalized)
            ? Result.Success(new TeamKey(normalized))
            : Result.Failure<TeamKey>(TeamKeyErrors.InvalidFormat);
    }

    /// <summary>
    /// Reconstruye una clave ya validada, sin volver a validarla.
    /// </summary>
    /// <remarks>
    /// Solo para materializar desde la base de datos: endurecer las reglas más adelante no
    /// debe impedir leer los equipos que ya existen.
    /// </remarks>
    public static TeamKey FromPersistence(string value) => new(value);

    public override string ToString() => Value;

    private static bool IsWellFormed(string value) =>
        char.IsAsciiLetterUpper(value[0]) &&
        value.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character));
}
