namespace Linear.Web.Features.Roadmaps.Contracts;

/// <summary>
/// Avance de una iniciativa, contado sobre los issues asociados.
/// </summary>
/// <param name="CompletionPercentage">
/// Porcentaje completado, redondeado. Una iniciativa sin issues es 0, no una división por
/// cero: todavía no hay trabajo con el que medirla.
/// </param>
public sealed record RoadmapItemProgress(int TotalIssues, int CompletedIssues, int CompletionPercentage)
{
    public static readonly RoadmapItemProgress Empty = new(0, 0, 0);

    // AwayFromZero y no el redondeo bancario que Math.Round usa por omisión, igual que en
    // las métricas de sprint: 1 de 8 tiene que dar 13 % y no 12 %.
    public static RoadmapItemProgress Create(int total, int completed) =>
        new(
            total,
            completed,
            total == 0 ? 0 : (int)Math.Round(completed * 100d / total, MidpointRounding.AwayFromZero));
}

/// <summary>
/// Una iniciativa del roadmap, con su avance.
/// </summary>
public sealed record RoadmapItemResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateOnly StartDate,
    DateOnly TargetDate,
    RoadmapItemProgress Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Un roadmap en un listado: sin las iniciativas, que en la lista no se dibujan.
/// </summary>
public sealed record RoadmapSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Un roadmap completo con sus iniciativas: es lo que alimenta la línea de tiempo.
/// </summary>
public sealed record RoadmapResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<RoadmapItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
