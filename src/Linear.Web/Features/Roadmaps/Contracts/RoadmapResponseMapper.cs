using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Roadmaps.Contracts;

/// <summary>
/// Arma las respuestas de la feature de Roadmaps.
/// </summary>
/// <remarks>
/// El avance de todas las iniciativas del roadmap se cuenta con una única consulta agrupada,
/// no una por iniciativa: la línea de tiempo las dibuja todas juntas, y una consulta por
/// barra sería exactamente el N+1 que el proyecto evita en los demás listados.
/// </remarks>
public static class RoadmapResponseMapper
{
    public static async Task<RoadmapResponse> ToResponseAsync(
        Roadmap roadmap,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roadmap);
        ArgumentNullException.ThrowIfNull(dbContext);

        var progress = await LoadProgressAsync(roadmap, dbContext, cancellationToken);

        var items = roadmap.Items
            // Por fecha de inicio: es el orden en que la línea de tiempo las apila.
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.CreatedAt)
            .Select(item => ToItemResponse(item, progress[item.Id]))
            .ToArray();

        return new RoadmapResponse(
            roadmap.Id,
            roadmap.Name,
            roadmap.Description,
            items,
            roadmap.CreatedAt,
            roadmap.UpdatedAt);
    }

    public static RoadmapSummaryResponse ToSummary(Roadmap roadmap)
    {
        ArgumentNullException.ThrowIfNull(roadmap);

        return new RoadmapSummaryResponse(
            roadmap.Id,
            roadmap.Name,
            roadmap.Description,
            roadmap.Items.Count,
            roadmap.CreatedAt,
            roadmap.UpdatedAt);
    }

    private static RoadmapItemResponse ToItemResponse(RoadmapItem item, RoadmapItemProgress progress) =>
        new(
            item.Id,
            item.Name,
            item.Description,
            item.Status.ToString(),
            item.StartDate,
            item.TargetDate,
            progress,
            item.CreatedAt,
            item.UpdatedAt);

    private static async Task<Dictionary<Guid, RoadmapItemProgress>> LoadProgressAsync(
        Roadmap roadmap,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var itemIds = roadmap.Items.Select(item => item.Id).ToArray();

        var progress = new Dictionary<Guid, RoadmapItemProgress>();

        if (itemIds.Length > 0)
        {
            var rows = await dbContext.Issues
                .AsNoTracking()
                .Where(issue => issue.RoadmapItemId != null && itemIds.Contains(issue.RoadmapItemId.Value))
                .GroupBy(issue => new { ItemId = issue.RoadmapItemId!.Value, issue.Status })
                .Select(group => new { group.Key.ItemId, group.Key.Status, Count = group.Count() })
                .ToArrayAsync(cancellationToken);

            foreach (var group in rows.GroupBy(row => row.ItemId))
            {
                progress[group.Key] = RoadmapItemProgress.Create(
                    group.Sum(row => row.Count),
                    group.Where(row => row.Status == IssueStatus.Done).Sum(row => row.Count));
            }
        }

        // Cada iniciativa aparece en el diccionario, tenga issues o no.
        foreach (var item in roadmap.Items)
        {
            progress.TryAdd(item.Id, RoadmapItemProgress.Empty);
        }

        return progress;
    }
}
