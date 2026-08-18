using Linear.Domain.Users;

using Microsoft.AspNetCore.Identity;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Implementación sobre <see cref="PasswordHasher{TUser}"/> de ASP.NET Core (PBKDF2 con
/// sal por contraseña y un factor de trabajo que el propio framework va actualizando).
/// </summary>
/// <remarks>
/// El tipo viene en el framework compartido, así que no agrega ninguna dependencia.
/// Se envuelve detrás de una interfaz propia para que los handlers no dependan del
/// espacio de nombres de Identity y para poder sustituirlo en los tests.
/// </remarks>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly User DummyUser = CreateDummyUser();

    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _hasher.HashPassword(user, password);
    }

    public PasswordVerification Verify(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(password))
        {
            return PasswordVerification.Failed;
        }

        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed
        };
    }

    public void VerifyDummy(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return;
        }

        _hasher.VerifyHashedPassword(DummyUser, DummyUser.PasswordHash, password);
    }

    private static User CreateDummyUser()
    {
        var email = Email.Create("dummy@linear.invalid").Value;
        var hasher = new PasswordHasher<User>();
        var user = User.Create(email, "dummy", UserRole.Member, "placeholder", DateTimeOffset.UnixEpoch).Value;

        user.ChangePasswordHash(hasher.HashPassword(user, Guid.NewGuid().ToString("N")), DateTimeOffset.UnixEpoch);

        return user;
    }
}
