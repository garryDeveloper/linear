using Linear.Domain.Users;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Revalida la cookie de sesión contra el estado actual del usuario.
/// </summary>
/// <remarks>
/// Sin esto, desactivar una cuenta o cambiarle el rol no tendría efecto hasta que venciera
/// la cookie, que dura dos semanas. El costo es una consulta por clave primaria en los
/// requests HTTP; el tráfico de un circuito Blazor viaja por SignalR y no lo paga.
/// </remarks>
public static class CookieValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userId = context.Principal.GetUserId();

        if (userId is null)
        {
            await RejectAsync(context);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var dbContext = services.GetRequiredService<AppDbContext>();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive)
        {
            services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(CookieValidator))
                .LogInformation("Se rechazó la sesión del usuario {UserId}: no existe o está desactivado.", userId);

            await RejectAsync(context);
            return;
        }

        if (HasStaleClaims(context, user))
        {
            // Renovar en lugar de rechazar: la sesión sigue siendo válida, solo cambió lo
            // que la identidad describe (nombre, rol o avatar).
            context.ReplacePrincipal(UserClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme));
            context.ShouldRenew = true;
        }
    }

    private static bool HasStaleClaims(CookieValidatePrincipalContext context, User user) =>
        context.Principal.GetRole() != user.Role ||
        !string.Equals(context.Principal.GetName(), user.Name, StringComparison.Ordinal) ||
        !string.Equals(context.Principal.GetEmail(), user.Email.Value, StringComparison.Ordinal) ||
        !string.Equals(context.Principal.GetAvatarUrl(), user.AvatarUrl, StringComparison.Ordinal);

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
