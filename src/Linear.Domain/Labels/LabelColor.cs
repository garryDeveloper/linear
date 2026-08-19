using System.Globalization;

using Linear.Domain.Common;

namespace Linear.Domain.Labels;

/// <summary>
/// Color de una label, en hexadecimal <c>#RRGGBB</c>.
/// </summary>
/// <remarks>
/// Se normaliza a mayúsculas y con almohadilla para que el mismo color no se guarde de
/// varias formas distintas, y para poder compararlo sin sorpresas.
/// </remarks>
public sealed record LabelColor
{
    /// <summary>Longitud de <c>#RRGGBB</c>.</summary>
    public const int Length = 7;

    /// <summary>Color de las labels que se crean sin elegir uno.</summary>
    public static LabelColor Default { get; } = new("#5B5BD6");

    private LabelColor(string value) => Value = value;

    public string Value { get; }

    public static Result<LabelColor> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LabelColor>(LabelColorErrors.Empty);
        }

        var normalized = value.Trim().TrimStart('#').ToUpperInvariant();

        if (normalized.Length != 6 || !normalized.All(char.IsAsciiHexDigit))
        {
            return Result.Failure<LabelColor>(LabelColorErrors.InvalidFormat);
        }

        return Result.Success(new LabelColor($"#{normalized}"));
    }

    /// <summary>
    /// Reconstruye un color ya validado, sin volver a validarlo.
    /// </summary>
    public static LabelColor FromPersistence(string value) => new(value);

    /// <summary>
    /// <summary>
    /// Indica si sobre este color conviene escribir en oscuro.
    /// </summary>
    /// <remarks>
    /// El usuario elige el color de fondo de la label, así que el texto tiene que adaptarse
    /// o queda ilegible. Se comparan los dos contrastes posibles según la fórmula de WCAG y
    /// se elige el mayor. Un umbral fijo sobre la luminancia parece equivalente pero no lo
    /// es: para un verde medio como #4CB782 da texto claro, cuando el negro contrasta más
    /// del triple.
    /// </remarks>
    public bool PrefersDarkText
    {
        get
        {
            var luminance = RelativeLuminance();

            var contrastWithBlack = (luminance + 0.05) / 0.05;
            var contrastWithWhite = 1.05 / (luminance + 0.05);

            return contrastWithBlack >= contrastWithWhite;
        }
    }

    public override string ToString() => Value;

    private double RelativeLuminance()
    {
        var red = Channel(1);
        var green = Channel(3);
        var blue = Channel(5);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private double Channel(int offset)
    {
        var value = byte.Parse(Value.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;

        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
