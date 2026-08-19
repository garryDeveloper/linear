using Linear.Domain.Issues;

using MudBlazor;

namespace Linear.Web.Components.Common;


/// <summary>
/// Nombres y marcas visuales de <see cref="IssueStatus"/> tal como se muestran en la
/// interfaz — un único lugar para que la lista, el tablero y el detalle usen siempre el
/// mismo glifo para el mismo estado.
/// </summary>
public static class IssueStatusLabel
{
    public static string For(string status) =>
        Enum.TryParse<IssueStatus>(status, out var parsed) ? For(parsed) : status;

    public static string For(IssueStatus status) => status switch
    {
        IssueStatus.Backlog => "Backlog",
        IssueStatus.Todo => "Todo",
        IssueStatus.InProgress => "En curso",
        IssueStatus.InReview => "En revisión",
        IssueStatus.Done => "Hecho",
        IssueStatus.Canceled => "Cancelado",
        _ => status.ToString()
    };

    public static string IconFor(IssueStatus status) => status switch
    {
        IssueStatus.InProgress => Icons.Material.Rounded.DonutLarge,
        IssueStatus.InReview => Icons.Material.Rounded.Visibility,
        IssueStatus.Done => Icons.Material.Rounded.CheckCircle,
        IssueStatus.Canceled => Icons.Material.Rounded.Cancel,
        _ => Icons.Material.Rounded.RadioButtonUnchecked
    };

    public static Color ColorFor(IssueStatus status) => status switch
    {
        IssueStatus.InProgress => Color.Primary,
        IssueStatus.InReview => Color.Warning,
        IssueStatus.Done => Color.Success,
        IssueStatus.Canceled => Color.Secondary,
        IssueStatus.Todo => Color.Default,
        _ => Color.Secondary
    };

    /// <summary>Orden en que se muestran los grupos de un listado agrupado por estado.</summary>
    public static readonly IReadOnlyList<IssueStatus> DisplayOrder =
    [
        IssueStatus.InProgress,
        IssueStatus.InReview,
        IssueStatus.Todo,
        IssueStatus.Backlog,
        IssueStatus.Done,
        IssueStatus.Canceled
    ];
}
