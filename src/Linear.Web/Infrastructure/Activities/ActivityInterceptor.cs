using System.Text.Json;

using Linear.Domain.Activities;
using Linear.Domain.Issues;
using Linear.Web.Infrastructure.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Linear.Web.Infrastructure.Activities;

/// <summary>
/// Convierte en filas de <see cref="Activity"/> lo que los agregados registraron, dentro del
/// mismo <c>SaveChanges</c> que guarda el cambio.
/// </summary>
/// <remarks>
/// Es el "mecanismo común" que pide la task 011, y está acá y no en cada feature a propósito:
/// ningún handler mencionaba Activity antes de esta task y ninguno la menciona después.
/// Agregar una acción nueva es levantar un evento en el agregado donde ocurre; no hay que
/// acordarse de tocar el slice.
///
/// Que sea un interceptor —y no un servicio que los handlers llamen— tiene una consecuencia
/// que importa: el registro se inserta en la misma transacción que el cambio. O se guardan
/// los dos, o no se guarda ninguno. Un historial que puede quedar desfasado del dato que
/// describe no serviría para auditar.
///
/// Es <c>Scoped</c> porque necesita saber quién está operando, y eso vive en el ámbito del
/// circuito o del request.
/// </remarks>
public sealed class ActivityInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is { } context)
        {
            await RecordAsync(context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task RecordAsync(DbContext context, CancellationToken cancellationToken)
    {
        var sources = context.ChangeTracker
            .Entries<IHasActivity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.PendingActivity.Count > 0)
            .ToArray();

        if (sources.Length == 0)
        {
            return;
        }

        // Sin sesión no se registra nada. Pasa en el seeder y en las migraciones, que crean
        // datos sin que haya un usuario detrás: inventarle un actor al historial sería peor
        // que no tenerlo.
        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            foreach (var source in sources)
            {
                source.ClearActivity();
            }

            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var source in sources)
        {
            foreach (var pending in source.PendingActivity)
            {
                var teamId = await ResolveTeamAsync(context, pending, cancellationToken);

                if (teamId is not { } team)
                {
                    continue;
                }

                context.Add(Activity.Record(
                    team,
                    userId.Value,
                    pending.EntityType,
                    pending.EntityId,
                    pending.Action,
                    Serialize(pending),
                    now));
            }

            // Se limpian siempre: si el mismo agregado se vuelve a guardar, no tiene que
            // registrar dos veces lo mismo.
            source.ClearActivity();
        }
    }

    /// <summary>
    /// Completa el equipo cuando el agregado no lo conoce.
    /// </summary>
    /// <remarks>
    /// Solo pasa con los comentarios, que únicamente saben su issue. Se busca primero entre
    /// las entidades ya rastreadas —lo habitual, porque el handler acaba de resolver el
    /// issue— y recién si no está se consulta la base.
    /// </remarks>
    private static async Task<Guid?> ResolveTeamAsync(
        DbContext context,
        ActivityEvent pending,
        CancellationToken cancellationToken)
    {
        if (pending.TeamId is { } known)
        {
            return known;
        }

        if (pending.IssueId is not { } issueId)
        {
            return null;
        }

        var tracked = context.ChangeTracker
            .Entries<Issue>()
            .FirstOrDefault(entry => entry.Entity.Id == issueId);

        if (tracked is not null)
        {
            return tracked.Entity.TeamId;
        }

        return await context.Set<Issue>()
            .AsNoTracking()
            .Where(issue => issue.Id == issueId)
            .Select(issue => (Guid?)issue.TeamId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Arma el payload. El issue al que pertenece la acción viaja adentro, y es lo que
    /// permite armar el historial de un issue incluyendo sus comentarios sin agregarle una
    /// columna a la tabla que la task no define.
    /// </summary>
    private static string Serialize(ActivityEvent pending)
    {
        var payload = new Dictionary<string, string?>(pending.Payload);

        if (pending.IssueId is { } issueId)
        {
            payload[ActivityPayload.IssueId] = issueId.ToString();
        }

        return JsonSerializer.Serialize(payload);
    }
}

/// <summary>Claves del payload que la aplicación consulta por nombre.</summary>
public static class ActivityPayload
{
    public const string IssueId = "issueId";

    /// <summary>
    /// Fragmento JSON para preguntar "esta actividad es de tal issue" con el operador de
    /// contención de jsonb.
    /// </summary>
    public static string IssueFragment(Guid issueId) =>
        JsonSerializer.Serialize(new Dictionary<string, string?> { [IssueId] = issueId.ToString() });
}
