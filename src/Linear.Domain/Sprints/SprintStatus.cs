namespace Linear.Domain.Sprints;

/// <summary>
/// Momento del ciclo de vida en que está un sprint.
/// </summary>
/// <remarks>
/// El recorrido es <see cref="Planned"/> → <see cref="Active"/> → <see cref="Completed"/>,
/// y desde cualquiera de los dos primeros se puede ir a <see cref="Canceled"/>.
/// <see cref="Completed"/> y <see cref="Canceled"/> son terminales: un sprint cerrado es
/// historia y ya no cambia.
/// </remarks>
public enum SprintStatus
{
    Planned = 0,
    Active = 1,
    Completed = 2,
    Canceled = 3
}
