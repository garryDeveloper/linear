using System.Security.Claims;

using Linear.Domain.Common;
using Linear.Domain.Users;

using Microsoft.AspNetCore.Components.Authorization;

namespace Linear.Web.Infrastructure.Authentication;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider services) : ICurrentUser
{
    public async ValueTask<ClaimsPrincipal?> GetPrincipalAsync(CancellationToken cancellationToken)
    {
        // Un request HTTP —endpoints y páginas de render estático— trae la identidad acá.
        if (httpContextAccessor.HttpContext?.User is { } principal)
        {
            return principal;
        }

        // Dentro de un circuito Blazor no hay HttpContext: la identidad la conserva el
        // proveedor de estado de autenticación, que vive en el ámbito del circuito.
        var stateProvider = services.GetService<AuthenticationStateProvider>();

        if (stateProvider is null)
        {
            return null;
        }

        try
        {
            var state = await stateProvider.GetAuthenticationStateAsync().WaitAsync(cancellationToken);

            return state.User;
        }
        catch (InvalidOperationException)
        {
            // El proveedor de Blazor exige que se lo consulte dentro del ámbito de un
            // componente y lanza si no. Eso ocurre cuando algo guarda datos fuera de la
            // interfaz y fuera de un request: la siembra al arrancar, un test que prepara
            // su escenario. Ahí la respuesta correcta es "no hay nadie", no una excepción:
            // quien pregunta ya contempla el caso —el historial no registra sin actor— y
            // hacerlo estallar convertiría la falta de sesión en una falla de la operación.
            return null;
        }
    }

    public async ValueTask<Result<Guid>> RequireIdAsync(CancellationToken cancellationToken)
    {
        var principal = await GetPrincipalAsync(cancellationToken);

        return principal.GetUserId() is { } userId
            ? Result.Success(userId)
            : Result.Failure<Guid>(UserErrors.NotAuthenticated);
    }
}
