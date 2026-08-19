using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Sprints.Contracts;

/// <summary>
/// Arma las respuestas de la feature de Sprints.
/// </summary>
/// <remarks>
/// El listado cuenta los issues de todos los sprints de la página con una única consulta
/// agrupada, no una por sprint. El detalle sí trae los issues completos: el tablero los
/// necesita a todos para ser un tablero, y un sprint es por definición un lote acotado de
/// trabajo —no el listado abierto del equipo, que sigue paginado—.
/// </remarks>
public static class SprintResponseMapper
{
    public static async Task<SprintResponse> ToResponseAsync(
        Sprint sprint,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        ArgumentNullException.ThrowIfNull(dbContext);

        // Sin Include de labels acá: ToSummariesAsync las carga aparte, por el mismo motivo
        // que en el listado de issues (un JOIN sobre una colección duplica filas).
        var issues = await dbContext.Issues
            .AsNoTracking()
            .Where(issue => issue.SprintId == sprint.Id)
            .OrderBy(issue => issue.CreatedAt)
            .ToArrayAsync(cancellationToken);

        var summaries = await IssueResponseMapper.ToSummariesAsync(issues, dbContext, cancellationToken);

        return new SprintResponse(
            sprint.Id,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status.ToString(),
            SprintMetrics.Create(issues.Length, issues.Count(issue => issue.Status == IssueStatus.Done)),
            summaries,
            sprint.CreatedAt,
            sprint.UpdatedAt,
            sprint.CompletedAt);
    }

    public static async Task<IReadOnlyList<SprintSummaryResponse>> ToSummariesAsync(
        IReadOnlyList<Sprint> sprints,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sprints);
        ArgumentNullException.ThrowIfNull(dbContext);

        if (sprints.Count == 0)
        {
            return [];
        }

        var metrics = await LoadMetricsAsync(sprints, dbContext, cancellationToken);

        return sprints
            .Select(sprint => new SprintSummaryResponse(
                sprint.Id,
                sprint.Name,
                sprint.Goal,
                sprint.StartDate,
                sprint.EndDate,
                sprint.Status.ToString(),
                metrics[sprint.Id],
                sprint.CreatedAt,
                sprint.CompletedAt))
            .ToArray();
    }

    private static async Task<Dictionary<Guid, SprintMetrics>> LoadMetricsAsync(
        IReadOnlyList<Sprint> sprints,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var sprintIds = sprints.Select(sprint => sprint.Id).ToArray();

        var rows = await dbContext.Issues
            .AsNoTracking()
            .Where(issue => issue.SprintId != null && sprintIds.Contains(issue.SprintId.Value))
            .GroupBy(issue => new { SprintId = issue.SprintId!.Value, issue.Status })
            .Select(group => new { group.Key.SprintId, group.Key.Status, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var metrics = rows
            .GroupBy(row => row.SprintId)
            .ToDictionary(
                group => group.Key,
                group => SprintMetrics.Create(
                    group.Sum(row => row.Count),
                    group.Where(row => row.Status == IssueStatus.Done).Sum(row => row.Count)));

        // Cada sprint aparece en el diccionario, tenga issues o no.
        foreach (var sprint in sprints)
        {
            metrics.TryAdd(sprint.Id, SprintMetrics.Empty);
        }

        return metrics;
    }
}
