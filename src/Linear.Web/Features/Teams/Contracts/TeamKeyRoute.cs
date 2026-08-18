using Linear.Domain.Common;
using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.Contracts;

/// <summary>
/// Interpreta la clave de equipo que llega en la ruta.
/// </summary>
public static class TeamKeyRoute
{
    /// <summary>
    /// Una clave mal formada no puede corresponder a ningún equipo, así que se responde
    /// "no existe" en lugar de un error de validación: la ruta no llegó a identificar nada.
    /// </summary>
    public static Result<TeamKey> Parse(string? value)
    {
        var key = TeamKey.Create(value);

        return key.IsSuccess
            ? key
            : Result.Failure<TeamKey>(TeamErrors.NotFoundByKey(value ?? string.Empty));
    }
}
