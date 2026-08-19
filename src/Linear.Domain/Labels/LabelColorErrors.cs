using Linear.Domain.Common;

namespace Linear.Domain.Labels;

public static class LabelColorErrors
{
    public static readonly Error Empty =
        Error.Validation("LabelColor.Empty", "El color es obligatorio.");

    public static readonly Error InvalidFormat = Error.Validation(
        "LabelColor.InvalidFormat",
        "El color debe ser hexadecimal, con el formato #RRGGBB.");
}
