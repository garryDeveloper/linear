using System.Security.Claims;

using Linear.Domain.Users;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Traduce entre un <see cref="User"/> y el <see cref="ClaimsPrincipal"/> que viaja en la cookie.
/// </summary>
/// <remarks>
/// Los claims llevan lo justo para renderizar la interfaz sin volver a consultar la base
/// en cada request. Nada que dependa de permisos por equipo se guarda acá: eso cambia con
/// frecuencia y debe leerse de la base en el momento de autorizar.
/// </remarks>
public static class UserClaims
{
    public const string AvatarUrl = "linear:avatar_url";

    public static ClaimsPrincipal CreatePrincipal(User user, string authenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        var claims = new List<Claim>(5)
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            claims.Add(new Claim(AvatarUrl, user.AvatarUrl));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationScheme));
    }
}
