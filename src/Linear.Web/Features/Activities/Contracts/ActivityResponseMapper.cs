using System.Text.Json;

using Linear.Domain.Activities;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Activities.Contracts;

/// <summary>
/// Arma las respuestas del historial.
/// </summary>
/// <remarks>
/// Los actores se cargan de una sola consulta para toda la página, no uno por entrada: un
/// feed de cincuenta líneas escrito por cuatro personas no debería costar cincuenta consultas.
/// </remarks>
public static class ActivityResponseMapper
{
    private static readonly IReadOnlyDictionary<string, string?> EmptyPayload =
        new Dictionary<string, string?>();

    public static async Task<IReadOnlyList<ActivityResponse>> ToResponsesAsync(
        IReadOnlyList<Activity> activities,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(dbContext);

        if (activities.Count == 0)
        {
            return [];
        }

        var actorIds = activities.Select(activity => activity.UserId).ToHashSet();

        var actors = await dbContext.Users
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => new IssueUserResponse(user.Id, user.Name, user.AvatarUrl),
                cancellationToken);

        return
        [
            .. activities.Select(activity => new ActivityResponse(
                activity.Id,
                activity.EntityType.ToString(),
                activity.EntityId,
                activity.Action.ToString(),
                // El actor no se puede borrar mientras tenga historial —la clave foránea es
                // Restrict—, pero si faltara, la entrada se muestra igual: perder el nombre
                // de quien lo hizo no debería borrar el hecho de que pasó.
                actors.TryGetValue(activity.UserId, out var actor)
                    ? actor
                    : new IssueUserResponse(activity.UserId, "Usuario desconocido", null),
                Deserialize(activity.PayloadJson),
                activity.CreatedAt))
        ];
    }

    /// <summary>
    /// Interpreta el payload. Un JSON que no se pueda leer no rompe el feed: se muestra la
    /// entrada sin detalle, que es más útil que una pantalla de error.
    /// </summary>
    private static IReadOnlyDictionary<string, string?> Deserialize(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return EmptyPayload;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(payloadJson) ?? EmptyPayload;
        }
        catch (JsonException)
        {
            return EmptyPayload;
        }
    }
}
