using System.Runtime.CompilerServices;
using System.Text.Json;

using Linear.Domain.Activities;
using Linear.Domain.Comments;
using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Web.Infrastructure.Activities;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Shared.Realtime;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Linear.Web.Infrastructure.Realtime;

/// <summary>
/// Convierte en avisos de tiempo real lo que se está guardando.
/// </summary>
/// <remarks>
/// Está donde está por el mismo motivo que el de actividad: ningún handler tiene que
/// acordarse de avisar. Una operación nueva emite avisos por el solo hecho de guardar.
/// <para>
/// La diferencia con aquel es <b>cuándo</b> hace su trabajo. La actividad se escribe
/// <i>antes</i> de confirmar, porque tiene que viajar en la misma transacción. Los avisos se
/// emiten <i>después</i>, y solo si la transacción salió bien: un aviso no se puede deshacer,
/// y anunciar un cambio que después se revirtió deja a todos los clientes mostrando algo que
/// nunca pasó. Por eso se calculan en <c>SavingChanges</c> —única oportunidad de ver qué
/// cambió— y se emiten en <c>SavedChanges</c>.
/// </para>
/// </remarks>
public sealed class RealtimeInterceptor(
    ITeamNotifier notifier,
    ICurrentUser currentUser) : SaveChangesInterceptor
{
    /// <summary>
    /// Lo calculado en <c>SavingChanges</c>, esperando a que la transacción confirme.
    /// </summary>
    /// <remarks>
    /// Va por contexto y no en un campo porque el interceptor es compartido: dentro de un
    /// mismo circuito puede haber dos operaciones guardando a la vez, cada una con su
    /// contexto. La tabla es débil para que un contexto descartado no quede retenido acá.
    /// </remarks>
    private readonly ConditionalWeakTable<DbContext, List<TeamNotification>> _pending = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is { } context)
        {
            var notifications = await CollectAsync(context, cancellationToken);

            if (notifications.Count > 0)
            {
                _pending.AddOrUpdate(context, notifications);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is { } context && _pending.TryGetValue(context, out var notifications))
        {
            _pending.Remove(context);

            await notifier.PublishAsync(notifications, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // El cambio no se guardó: no hay nada que anunciar.
        if (eventData.Context is { } context)
        {
            _pending.Remove(context);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private async Task<List<TeamNotification>> CollectAsync(
        DbContext context,
        CancellationToken cancellationToken)
    {
        var notifications = new List<TeamNotification>();

        foreach (var entry in context.ChangeTracker.Entries<Issue>())
        {
            if (Describe(entry) is { } notification)
            {
                notifications.Add(notification);
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<Comment>())
        {
            var notification = await DescribeAsync(entry, context, cancellationToken);

            if (notification is not null)
            {
                notifications.Add(notification);
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<Sprint>())
        {
            if (Describe(entry) is { } notification)
            {
                notifications.Add(notification);
            }
        }

        // Las actividades las agrega el interceptor de la task 011 durante este mismo
        // SavingChanges. Verlas acá depende de que aquel corra primero, y por eso el orden
        // de registro está fijado —y verificado por un test— en AddPersistence.
        foreach (var entry in context.ChangeTracker.Entries<Activity>())
        {
            if (entry.State is not EntityState.Added)
            {
                continue;
            }

            var issueId = IssueOf(entry.Entity);

            // El identificador legible se resuelve acá, igual que para los comentarios, para
            // que el historial de un issue pueda descartar lo que no le corresponde sin
            // consultar la base en cada aviso.
            var issue = issueId is { } id
                ? await ResolveIssueAsync(context, id, cancellationToken)
                : null;

            notifications.Add(new TeamNotification
            {
                Event = RealtimeEvent.ActivityCreated,
                TeamId = entry.Entity.TeamId,
                IssueId = issueId,
                Identifier = issue?.Identifier,
                EntityId = entry.Entity.Id
            });
        }

        if (notifications.Count == 0)
        {
            return notifications;
        }

        // El autor se resuelve una sola vez, y recién cuando hay algo que anunciar: la
        // enorme mayoría de los guardados no produce ningún aviso, y preguntarlo antes haría
        // pagar esa consulta a todos.
        //
        // Puede no haber sesión —la siembra guarda datos sin usuario detrás—. El aviso se
        // emite igual, porque el cambio ocurrió; simplemente no hay a quién evitarle el eco.
        var actor = await currentUser.RequireIdAsync(cancellationToken);

        if (actor.IsFailure)
        {
            return notifications;
        }

        return [.. notifications.Select(notification => notification with { ActorUserId = actor.Value })];
    }

    private static TeamNotification? Describe(EntityEntry<Issue> entry)
    {
        var issue = entry.Entity;

        var kind = entry.State switch
        {
            EntityState.Added => RealtimeEvent.IssueCreated,

            // Cubre todo lo que la task enumera por separado —estado, prioridad, responsable,
            // labels, estimación, archivado—. Todas las mutaciones pasan por el agregado y
            // todas tocan UpdatedAt, así que ninguna se escapa. Enumerarlas acá sería repetir
            // la lista de métodos de Issue y desactualizarse con el primero que se agregue.
            EntityState.Modified => RealtimeEvent.IssueUpdated,
            EntityState.Deleted => RealtimeEvent.IssueDeleted,
            _ => (RealtimeEvent?)null
        };

        if (kind is not { } happened)
        {
            return null;
        }

        return new TeamNotification
        {
            Event = happened,
            TeamId = issue.TeamId,
            IssueId = issue.Id,
            Identifier = issue.Identifier.Value,
            EntityId = issue.Id
        };
    }

    private static TeamNotification? Describe(EntityEntry<Sprint> entry)
    {
        // Crear, editar, arrancar, completar y cancelar son para un cliente conectado la
        // misma noticia: el sprint cambió, hay que volver a pedirlo.
        if (entry.State is not (EntityState.Added or EntityState.Modified))
        {
            return null;
        }

        return new TeamNotification
        {
            Event = RealtimeEvent.SprintUpdated,
            TeamId = entry.Entity.TeamId,
            EntityId = entry.Entity.Id
        };
    }

    private static async Task<TeamNotification?> DescribeAsync(
        EntityEntry<Comment> entry,
        DbContext context,
        CancellationToken cancellationToken)
    {
        var comment = entry.Entity;

        var kind = entry.State switch
        {
            EntityState.Added => RealtimeEvent.CommentCreated,

            // Eliminar un comentario es marcarlo, no borrar la fila: para EF es una
            // modificación como cualquier otra y hay que mirar la columna para distinguirla.
            EntityState.Modified when JustDeleted(entry) => RealtimeEvent.CommentDeleted,
            EntityState.Modified => RealtimeEvent.CommentUpdated,
            _ => (RealtimeEvent?)null
        };

        if (kind is not { } happened)
        {
            return null;
        }

        var issue = await ResolveIssueAsync(context, comment.IssueId, cancellationToken);

        if (issue is not { } owner)
        {
            return null;
        }

        return new TeamNotification
        {
            Event = happened,
            TeamId = owner.TeamId,
            IssueId = comment.IssueId,
            Identifier = owner.Identifier,
            EntityId = comment.Id
        };
    }

    /// <summary>Si en este guardado el comentario pasó de vivo a eliminado.</summary>
    private static bool JustDeleted(EntityEntry<Comment> entry)
    {
        var property = entry.Property(comment => comment.DeletedAt);

        return property.IsModified && property.OriginalValue is null && property.CurrentValue is not null;
    }

    /// <summary>Lo que un comentario necesita saber de su issue para poder anunciarse.</summary>
    private readonly record struct IssueOwner(Guid TeamId, string Identifier);

    /// <summary>
    /// Equipo e identificador del issue.
    /// </summary>
    /// <remarks>
    /// Un comentario solo conoce el identificador interno de su issue, y hacen falta las dos
    /// cosas: el equipo para saber a qué grupo emitir, y el identificador legible para que la
    /// pantalla que muestra el hilo pueda descartar el aviso sin consultar la base.
    /// <para>
    /// Se busca primero entre lo ya rastreado —lo habitual, porque el handler acaba de
    /// resolver el issue— y recién si no está se consulta.
    /// </para>
    /// </remarks>
    private static async Task<IssueOwner?> ResolveIssueAsync(
        DbContext context,
        Guid issueId,
        CancellationToken cancellationToken)
    {
        var tracked = context.ChangeTracker
            .Entries<Issue>()
            .FirstOrDefault(entry => entry.Entity.Id == issueId);

        if (tracked is not null)
        {
            return new IssueOwner(tracked.Entity.TeamId, tracked.Entity.Identifier.Value);
        }

        return await context.Set<Issue>()
            .AsNoTracking()
            .Where(issue => issue.Id == issueId)
            .Select(issue => (IssueOwner?)new IssueOwner(issue.TeamId, issue.Identifier.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Issue al que se refiere una actividad, si se refiere a alguno.
    /// </summary>
    /// <remarks>
    /// Viene dentro del payload, que es donde la task 011 lo dejó. Leerlo permite que el
    /// historial de un issue se refresque solo cuando le toca, en vez de hacerlo ante
    /// cualquier actividad del equipo.
    /// </remarks>
    private static Guid? IssueOf(Activity activity)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string?>>(activity.PayloadJson);

            if (payload?.TryGetValue(ActivityPayload.IssueId, out var raw) == true &&
                Guid.TryParse(raw, out var issueId))
            {
                return issueId;
            }
        }
        catch (JsonException)
        {
            // Un payload ilegible no puede impedir que se avise del cambio.
        }

        return null;
    }
}
