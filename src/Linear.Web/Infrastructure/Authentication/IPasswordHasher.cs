using Linear.Domain.Users;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Calcula y verifica hashes de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    string Hash(User user, string password);

    PasswordVerification Verify(User user, string password);

    /// <summary>
    /// Verifica contra un hash descartable.
    /// </summary>
    /// <remarks>
    /// Se usa cuando el email no corresponde a ningún usuario: sin este trabajo extra,
    /// una respuesta notoriamente más rápida delataría qué direcciones están registradas.
    /// </remarks>
    void VerifyDummy(string password);
}

public enum PasswordVerification
{
    Failed = 0,
    Success = 1,

    /// <summary>La contraseña es correcta pero el hash usa parámetros viejos.</summary>
    SuccessRehashNeeded = 2
}
