namespace Linear.Domain.Roadmaps;

/// <summary>
/// Estado de una iniciativa del roadmap.
/// </summary>
/// <remarks>
/// A diferencia de <c>SprintStatus</c>, acá no hay un recorrido obligatorio: una iniciativa
/// puede volver de <see cref="InProgress"/> a <see cref="Planned"/> si se despriorizó, o
/// reabrirse después de darse por terminada. El roadmap es una intención, no un proceso con
/// pasos, y la task 010 no define transiciones.
/// </remarks>
public enum RoadmapItemStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Canceled = 3
}
