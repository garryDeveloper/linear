using Linear.Domain.Issues;
using Linear.Web.Features.Issues.Filtering;

namespace Linear.Web.Components.Common;

/// <summary>Un valor posible de un filtro, con el texto que se muestra.</summary>
public sealed record IssueFilterOption(string Value, string Label);

/// <summary>
/// Nombres en castellano de los campos y operadores de filtrado, y los valores fijos que
/// cada campo ofrece.
/// </summary>
/// <remarks>
/// Las opciones que dependen del equipo —miembros, labels, sprints— las aporta el
/// constructor de filtros, que es quien las tiene cargadas.
/// </remarks>
public static class IssueFilterOptions
{
    /// <summary>Valor especial: el usuario de la sesión.</summary>
    public const string Me = "me";

    /// <summary>Valor especial: sin responsable, o sin sprint, según el campo.</summary>
    public const string None = "none";

    public static string For(IssueFilterField field) => field switch
    {
        IssueFilterField.Status => "Estado",
        IssueFilterField.Priority => "Prioridad",
        IssueFilterField.Assignee => "Responsable",
        IssueFilterField.Label => "Label",
        IssueFilterField.Sprint => "Sprint",
        IssueFilterField.CreatedBy => "Creado por",
        IssueFilterField.RoadmapItem => "Iniciativa",
        IssueFilterField.Title => "Título",
        _ => field.ToString()
    };

    public static string IconFor(IssueFilterField field) => field switch
    {
        IssueFilterField.Status => MudBlazor.Icons.Material.Rounded.RadioButtonUnchecked,
        IssueFilterField.Priority => MudBlazor.Icons.Material.Rounded.Flag,
        IssueFilterField.Assignee => MudBlazor.Icons.Material.Rounded.Person,
        IssueFilterField.Label => MudBlazor.Icons.Material.Rounded.Label,
        IssueFilterField.Sprint => MudBlazor.Icons.Material.Rounded.DateRange,
        IssueFilterField.CreatedBy => MudBlazor.Icons.Material.Rounded.PersonAdd,
        IssueFilterField.RoadmapItem => MudBlazor.Icons.Material.Rounded.Timeline,
        IssueFilterField.Title => MudBlazor.Icons.Material.Rounded.Title,
        _ => MudBlazor.Icons.Material.Rounded.FilterList
    };

    /// <summary>
    /// Verbo del operador. Depende de cuántos valores haya elegidos, porque "es" con varios
    /// valores se lee "está en" — es la misma condición, no dos operadores distintos.
    /// </summary>
    public static string OperatorFor(IssueFilterField field, bool negated, int valueCount)
    {
        if (field.IsText())
        {
            return "contiene";
        }

        return (negated, valueCount > 1) switch
        {
            (false, false) => "es",
            (false, true) => "está en",
            (true, false) => "no es",
            (true, true) => "no está en"
        };
    }

    /// <summary>Valores fijos de un campo, los que no dependen del equipo.</summary>
    public static IReadOnlyList<IssueFilterOption> FixedOptionsFor(IssueFilterField field) => field switch
    {
        IssueFilterField.Status =>
        [
            .. Enum.GetValues<IssueStatus>()
                .Select(status => new IssueFilterOption(status.ToString(), IssueStatusLabel.For(status)))
        ],
        IssueFilterField.Priority =>
        [
            .. Enum.GetValues<IssuePriority>()
                .Select(priority => new IssueFilterOption(priority.ToString(), IssuePriorityLabel.For(priority)))
        ],
        _ => []
    };
}
