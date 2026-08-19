namespace Linear.Domain.Issues;

/// <summary>
/// Urgencia de un issue.
/// </summary>
/// <remarks>
/// El orden de los valores importa: se usa para ordenar listados de mayor a menor
/// urgencia sin una tabla de traducción aparte.
/// </remarks>
public enum IssuePriority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}
