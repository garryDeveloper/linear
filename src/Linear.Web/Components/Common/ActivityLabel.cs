using Linear.Domain.Activities;
using Linear.Domain.Issues;
using Linear.Web.Features.Activities.Contracts;

using MudBlazor;

namespace Linear.Web.Components.Common;

/// <summary>
/// Convierte una entrada del historial en una frase en castellano.
/// </summary>
/// <remarks>
/// La frase se arma acá y no en el servidor porque es presentación: el historial guarda qué
/// pasó, no cómo se cuenta. Eso además deja traducirlo o reformularlo sin migrar datos, que
/// es importante en una tabla que es append-only.
///
/// Una acción que este código no conozca —porque la escribió una versión más nueva— se
/// muestra igual con su nombre crudo, en vez de desaparecer del feed.
/// </remarks>
public static class ActivityLabel
{
    /// <summary>Frase que describe la acción, sin el nombre del actor adelante.</summary>
    public static string For(ActivityResponse activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var action = Parse(activity.Action);

        return action switch
        {
            ActivityAction.IssueCreated => "creó el issue",
            ActivityAction.IssueUpdated => DescribeUpdate(activity),
            ActivityAction.IssueAssigned => Value(activity, "newValue") is null
                ? "quitó el responsable"
                : "cambió el responsable",
            ActivityAction.IssueCompleted => "completó el issue",
            ActivityAction.IssueCanceled => "canceló el issue",

            ActivityAction.CommentCreated => "comentó",
            ActivityAction.CommentUpdated => "editó un comentario",

            ActivityAction.LabelAdded => "agregó una label",
            ActivityAction.LabelRemoved => "quitó una label",

            ActivityAction.SprintStarted => $"inició el sprint «{Value(activity, "name")}»",
            ActivityAction.SprintCompleted => $"completó el sprint «{Value(activity, "name")}»",

            ActivityAction.RoadmapItemCreated => $"creó la iniciativa «{Value(activity, "name")}»",
            ActivityAction.RoadmapItemUpdated => $"actualizó la iniciativa «{Value(activity, "name")}»",

            _ => activity.Action
        };
    }

    /// <summary>
    /// Detalle secundario, cuando lo hay: el paso de un estado a otro, por ejemplo. Se
    /// muestra aparte para que la frase principal se lea corta.
    /// </summary>
    public static string? DetailFor(ActivityResponse activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (Parse(activity.Action) is not (ActivityAction.IssueUpdated
            or ActivityAction.IssueCompleted
            or ActivityAction.IssueCanceled))
        {
            return null;
        }

        if (Value(activity, "field") != nameof(Issue.Status))
        {
            return null;
        }

        var from = Value(activity, "oldValue");
        var to = Value(activity, "newValue");

        return from is null || to is null
            ? null
            : $"{IssueStatusLabel.For(from)} → {IssueStatusLabel.For(to)}";
    }

    public static string IconFor(ActivityResponse activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return Parse(activity.Action) switch
        {
            ActivityAction.IssueCreated => Icons.Material.Rounded.AddCircleOutline,
            ActivityAction.IssueAssigned => Icons.Material.Rounded.Person,
            ActivityAction.IssueCompleted => Icons.Material.Rounded.CheckCircle,
            ActivityAction.IssueCanceled => Icons.Material.Rounded.Cancel,
            ActivityAction.CommentCreated or ActivityAction.CommentUpdated =>
                Icons.Material.Rounded.ChatBubbleOutline,
            ActivityAction.LabelAdded or ActivityAction.LabelRemoved => Icons.Material.Rounded.Label,
            ActivityAction.SprintStarted or ActivityAction.SprintCompleted =>
                Icons.Material.Rounded.DateRange,
            ActivityAction.RoadmapItemCreated or ActivityAction.RoadmapItemUpdated =>
                Icons.Material.Rounded.Timeline,
            _ => Icons.Material.Rounded.Edit
        };
    }

    /// <summary>
    /// Identificador del issue con el que se relaciona la entrada, para poder enlazarla.
    /// </summary>
    public static string? IssueIdentifierOf(ActivityResponse activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return Value(activity, "identifier");
    }

    /// <summary>Tiempo transcurrido, en la forma en que se lee un feed.</summary>
    public static string Elapsed(DateTimeOffset moment, DateTimeOffset now)
    {
        var elapsed = now - moment;

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "recién",
            { TotalMinutes: < 60 } => $"hace {(int)elapsed.TotalMinutes} min",
            { TotalHours: < 24 } => $"hace {(int)elapsed.TotalHours} h",
            { TotalDays: < 30 } => $"hace {(int)elapsed.TotalDays} d",
            _ => moment.ToLocalTime().ToString("dd/MM/yyyy")
        };
    }

    private static string DescribeUpdate(ActivityResponse activity) =>
        Value(activity, "field") == nameof(Issue.Status)
            ? "cambió el estado"
            : "editó el issue";

    private static string? Value(ActivityResponse activity, string key) =>
        activity.Payload.TryGetValue(key, out var value) ? value : null;

    private static ActivityAction? Parse(string action) =>
        Enum.TryParse<ActivityAction>(action, out var parsed) ? parsed : null;
}
