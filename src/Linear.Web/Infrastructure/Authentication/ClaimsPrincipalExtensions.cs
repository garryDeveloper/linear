using System.Security.Claims;

using Linear.Domain.Users;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Lectura de la identidad del usuario desde sus claims.
/// </summary>
/// <remarks>
/// Las usan tanto el código de servidor (a través de <see cref="ICurrentUser"/>) como los
/// componentes Blazor (sobre <c>AuthenticationState.User</c>), que en un circuito no tienen
/// acceso al <c>HttpContext</c>.
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true;

    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static string? GetName(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Name);

    public static string? GetEmail(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Email);

    public static string? GetAvatarUrl(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(UserClaims.AvatarUrl);

    public static UserRole? GetRole(this ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(ClaimTypes.Role);

        return Enum.TryParse<UserRole>(value, ignoreCase: false, out var role) ? role : null;
    }
}
