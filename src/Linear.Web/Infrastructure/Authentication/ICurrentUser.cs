using System.Security.Claims;

using Linear.Domain.Common;

namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Identidad del usuario que está ejecutando la operación en curso.
/// </summary>
/// <remarks>
/// Los handlers se invocan desde dos lugares con mecánicas distintas: un endpoint HTTP,
/// donde la identidad viaja en el <c>HttpContext</c>, y un componente Blazor, que corre
/// dentro de un circuito donde ese contexto ya no existe. Esta abstracción resuelve ambos
/// casos para que un handler no tenga que saber quién lo llamó.
/// Es asíncrona porque obtener el estado de autenticación de un circuito lo es.
/// </remarks>
public interface ICurrentUser
{
    ValueTask<ClaimsPrincipal?> GetPrincipalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Identificador del usuario autenticado, o un error si no hay sesión.
    /// </summary>
    ValueTask<Result<Guid>> RequireIdAsync(CancellationToken cancellationToken);
}
