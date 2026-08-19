using Linear.Domain.Sprints;

using MudBlazor;

namespace Linear.Web.Components.Common;

/// <summary>
/// Nombres y marcas visuales de <see cref="SprintStatus"/>, en un único lugar para que la
/// lista y el detalle usen siempre el mismo color para el mismo estado.
/// </summary>
public static class SprintStatusLabel
{
    public static string For(string status) =>
        Enum.TryParse<SprintStatus>(status, out var parsed) ? For(parsed) : status;

    public static string For(SprintStatus status) => status switch
    {
        SprintStatus.Planned => "Planificado",
        SprintStatus.Active => "En curso",
        SprintStatus.Completed => "Completado",
        SprintStatus.Canceled => "Cancelado",
        _ => status.ToString()
    };

    public static Color ColorFor(SprintStatus status) => status switch
    {
        SprintStatus.Active => Color.Primary,
        SprintStatus.Completed => Color.Success,
        SprintStatus.Canceled => Color.Secondary,
        _ => Color.Default
    };

    public static SprintStatus Parse(string status) =>
        Enum.TryParse<SprintStatus>(status, out var parsed) ? parsed : SprintStatus.Planned;

    /// <summary>
    /// Columnas del tablero de sprint, en el orden que define la task 007.
    /// </summary>
    /// <remarks>
    /// <c>Backlog</c> y <c>Canceled</c> no son columnas: el tablero muestra el trabajo
    /// comprometido y en qué punto está. Lo que esté en backlog se dibuja en Todo —las dos
    /// cosas significan "sin empezar"— y lo cancelado se cuenta aparte, para que ningún
    /// issue del sprint desaparezca sin dejar rastro.
    /// </remarks>
    public static readonly IReadOnlyList<Linear.Domain.Issues.IssueStatus> BoardColumns =
    [
        Linear.Domain.Issues.IssueStatus.Todo,
        Linear.Domain.Issues.IssueStatus.InProgress,
        Linear.Domain.Issues.IssueStatus.InReview,
        Linear.Domain.Issues.IssueStatus.Done
    ];
}
