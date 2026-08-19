using Linear.Domain.Teams;

namespace Linear.Domain.Issues;

/// <summary>
/// Identificador legible de un issue, por ejemplo <c>WEB-42</c>.
/// </summary>
/// <remarks>
/// A diferencia de <see cref="Email"/> o <see cref="TeamKey"/>, no valida una entrada de
/// usuario: el servidor es el único que lo construye, a partir de la clave del equipo y de
/// un número que asigna <c>IssueNumberSequence</c> de forma segura ante concurrencia. Por
/// eso no tiene una regla de formato que pueda fallar — solo formatea.
/// </remarks>
public sealed record IssueIdentifier
{
    private IssueIdentifier(string value) => Value = value;

    public string Value { get; }

    public static IssueIdentifier Create(TeamKey teamKey, int number)
    {
        ArgumentNullException.ThrowIfNull(teamKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        return new IssueIdentifier($"{teamKey.Value}-{number}");
    }

    /// <summary>
    /// Reconstruye un identificador ya persistido, sin volver a formatearlo.
    /// </summary>
    public static IssueIdentifier FromPersistence(string value) => new(value);

    public override string ToString() => Value;
}
