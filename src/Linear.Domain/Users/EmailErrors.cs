using Linear.Domain.Common;

namespace Linear.Domain.Users;

public static class EmailErrors
{
    public static readonly Error Empty =
        Error.Validation("Email.Empty", "El email es obligatorio.");

    public static readonly Error TooLong =
        Error.Validation("Email.TooLong", $"El email no puede superar los {Email.MaxLength} caracteres.");

    public static readonly Error InvalidFormat =
        Error.Validation("Email.InvalidFormat", "El email no tiene un formato válido.");
}
