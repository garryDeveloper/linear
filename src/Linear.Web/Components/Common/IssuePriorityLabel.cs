using Linear.Domain.Issues;

using MudBlazor;

namespace Linear.Web.Components.Common;


/// <summary>
/// Nombres y marcas visuales de <see cref="IssuePriority"/>.
/// </summary>
public static class IssuePriorityLabel
{
    public static string For(string priority) =>
        Enum.TryParse<IssuePriority>(priority, out var parsed) ? For(parsed) : priority;

    public static string For(IssuePriority priority) => priority switch
    {
        IssuePriority.None => "Sin prioridad",
        IssuePriority.Low => "Baja",
        IssuePriority.Medium => "Media",
        IssuePriority.High => "Alta",
        IssuePriority.Urgent => "Urgente",
        _ => priority.ToString()
    };

    public static string IconFor(IssuePriority priority) =>
        priority == IssuePriority.None ? Icons.Material.Rounded.Remove : Icons.Material.Rounded.Flag;

    public static Color ColorFor(IssuePriority priority) => priority switch
    {
        IssuePriority.Urgent => Color.Error,
        IssuePriority.High => Color.Warning,
        IssuePriority.Medium => Color.Info,
        _ => Color.Secondary
    };
}
