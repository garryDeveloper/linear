using Linear.Domain.Roadmaps;

namespace Linear.Web.Features.Roadmaps.UpdateItem;

public sealed class UpdateRoadmapItemRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetDate { get; set; }

    /// <summary>
    /// Estado de la iniciativa. Se edita junto con el resto: el roadmap no tiene un recorrido
    /// obligatorio de estados, así que no hace falta una operación aparte por transición.
    /// </summary>
    public RoadmapItemStatus Status { get; set; }
}
