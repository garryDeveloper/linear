using Linear.Domain.Roadmaps;

using MudBlazor;

namespace Linear.Web.Components.Common;

/// <summary>
/// Nombres y colores de <see cref="RoadmapItemStatus"/>, en un único lugar para que la
/// línea de tiempo y los formularios usen siempre el mismo para el mismo estado.
/// </summary>
public static class RoadmapItemStatusLabel
{
    public static string For(string status) =>
        Enum.TryParse<RoadmapItemStatus>(status, out var parsed) ? For(parsed) : status;

    public static string For(RoadmapItemStatus status) => status switch
    {
        RoadmapItemStatus.Planned => "Planificada",
        RoadmapItemStatus.InProgress => "En curso",
        RoadmapItemStatus.Completed => "Completada",
        RoadmapItemStatus.Canceled => "Cancelada",
        _ => status.ToString()
    };

    public static Color ColorFor(RoadmapItemStatus status) => status switch
    {
        RoadmapItemStatus.InProgress => Color.Primary,
        RoadmapItemStatus.Completed => Color.Success,
        RoadmapItemStatus.Canceled => Color.Secondary,
        _ => Color.Default
    };

    public static RoadmapItemStatus Parse(string status) =>
        Enum.TryParse<RoadmapItemStatus>(status, out var parsed) ? parsed : RoadmapItemStatus.Planned;
}
