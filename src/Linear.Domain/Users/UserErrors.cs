using Linear.Domain.Common;

namespace Linear.Domain.Users;

public static class UserErrors
{
    /// <summary>
    /// Se devuelve tanto cuando el email no existe como cuando la contraseña no coincide.
    /// Distinguir los dos casos permitiría averiguar qué direcciones están registradas.
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Users.InvalidCredentials", "El email o la contraseña no son correctos.");

    /// <summary>
    /// Solo se informa después de verificar la contraseña, para no revelar la existencia
    /// de la cuenta a quien no conoce las credenciales.
    /// </summary>
    public static readonly Error Inactive =
        Error.Forbidden("Users.Inactive", "La cuenta está desactivada.");

    public static readonly Error NotAuthenticated =
        Error.Unauthorized("Users.NotAuthenticated", "No hay una sesión iniciada.");

    public static readonly Error NameRequired =
        Error.Validation("Users.NameRequired", "El nombre es obligatorio.");

    public static readonly Error NameTooLong =
        Error.Validation("Users.NameTooLong", $"El nombre no puede superar los {User.MaxNameLength} caracteres.");

    public static readonly Error AvatarUrlTooLong =
        Error.Validation("Users.AvatarUrlTooLong", $"La URL del avatar no puede superar los {User.MaxAvatarUrlLength} caracteres.");

    public static readonly Error PasswordHashRequired =
        Error.Validation("Users.PasswordHashRequired", "La contraseña es obligatoria.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Users.EmailAlreadyExists", "Ya existe un usuario con ese email.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"No existe el usuario '{userId}'.");
}
