using Linear.Web.Features.Issues.Contracts;

namespace Linear.Web.Features.Sprints.Contracts;

/// <summary>
/// Avance del sprint, calculado en el servidor para que la lista, el detalle y el tablero
/// muestren siempre el mismo número.
/// </summary>
/// <param name="Total">Todos los issues asignados al sprint.</param>
/// <param name="Completed">Los que están en estado <c>Done</c>.</param>
/// <param name="Remaining">Los que faltan: <paramref name="Total"/> menos los completados.</param>
/// <param name="CompletionPercentage">
/// Porcentaje completado, redondeado a entero. Un sprint sin issues es 0, no una división
/// por cero.
/// </param>
public sealed record SprintMetrics(int Total, int Completed, int Remaining, int CompletionPercentage)
{
    public static readonly SprintMetrics Empty = new(0, 0, 0, 0);

    // AwayFromZero y no el redondeo bancario que Math.Round usa por omisión: con el valor
    // por defecto, 1 de 8 daría 12 % en vez de 13 %, que es lo que cualquiera espera ver.
    public static SprintMetrics Create(int total, int completed) =>
        new(
            total,
            completed,
            total - completed,
            total == 0 ? 0 : (int)Math.Round(completed * 100d / total, MidpointRounding.AwayFromZero));
}

/// <summary>
/// Un sprint en un listado: sin los issues, que en la lista no se muestran.
/// </summary>
public sealed record SprintSummaryResponse(
    Guid Id,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    SprintMetrics Metrics,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>
/// Un sprint completo, con los issues que contiene — es lo que alimenta el tablero.
/// </summary>
public sealed record SprintResponse(
    Guid Id,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    SprintMetrics Metrics,
    IReadOnlyList<IssueSummaryResponse> Issues,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);
