using Linear.Domain.Issues;
using Linear.Domain.Users;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Contracts;

/// <summary>
/// Arma las respuestas de la feature de Issues.
/// </summary>
/// <remarks>
/// El listado nunca combina <c>Include</c> con <c>Skip</c>/<c>Take</c>: un <c>Include</c>
/// sobre una colección se traduce en un JOIN, y un JOIN sobre una página ya recortada
/// duplica filas o desalinea el recorte. Por eso <see cref="ToSummariesAsync"/> trae los
/// issues sin sus labels y las carga aparte, acotadas a esa página — dos consultas en total,
/// no una por issue.
/// </remarks>
public static class IssueResponseMapper
{
    /// <summary>
    /// Arma la respuesta completa de un issue. Asume que <see cref="Issue.Labels"/> ya está
    /// cargada (vía <c>Include</c>): es una consulta de un único registro, no paginada.
    /// </summary>
    public static async Task<IssueResponse> ToResponseAsync(
        Issue issue,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(dbContext);

        var users = await LoadUsersAsync(dbContext, CollectUserIds([issue]), cancellationToken);
        var labels = await LoadLabelsAsync(dbContext, [issue], cancellationToken);
        var sprint = await LoadSprintAsync(dbContext, issue.SprintId, cancellationToken);
        var roadmapItem = await LoadRoadmapItemAsync(dbContext, issue.RoadmapItemId, cancellationToken);

        return ToResponse(issue, users, labels[issue.Id], sprint, roadmapItem);
    }

    /// <summary>
    /// Trae la iniciativa del roadmap a la que aporta el issue, si aporta a alguna, junto con
    /// el roadmap que la contiene.
    /// </summary>
    private static async Task<IssueRoadmapItemResponse?> LoadRoadmapItemAsync(
        AppDbContext dbContext,
        Guid? roadmapItemId,
        CancellationToken cancellationToken) =>
        // El filtro va antes de proyectar: filtrar sobre el record ya construido no se puede
        // traducir a SQL, porque EF no sabe volver de sus propiedades a las columnas.
        roadmapItemId is not { } id
            ? null
            : await dbContext.Roadmaps
                .AsNoTracking()
                .SelectMany(roadmap => roadmap.Items, (roadmap, item) => new { roadmap, item })
                .Where(row => row.item.Id == id)
                .Select(row => new IssueRoadmapItemResponse(
                    row.item.Id, row.item.Name, row.roadmap.Id, row.roadmap.Name))
                .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Trae el sprint del issue, si tiene. Solo el nombre: el detalle del issue lo muestra
    /// como una referencia, no necesita sus métricas.
    /// </summary>
    private static async Task<IssueSprintResponse?> LoadSprintAsync(
        AppDbContext dbContext,
        Guid? sprintId,
        CancellationToken cancellationToken) =>
        sprintId is not { } id
            ? null
            : await dbContext.Sprints
                .AsNoTracking()
                .Where(sprint => sprint.Id == id)
                .Select(sprint => new IssueSprintResponse(sprint.Id, sprint.Name))
                .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Arma las respuestas livianas de una página de issues.
    /// </summary>
    /// <param name="issues">Traídos sin <c>Include</c> de labels.</param>
    public static async Task<IReadOnlyList<IssueSummaryResponse>> ToSummariesAsync(
        IReadOnlyList<Issue> issues,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(dbContext);

        if (issues.Count == 0)
        {
            return [];
        }

        var users = await LoadUsersAsync(dbContext, CollectUserIds(issues), cancellationToken);
        var labelsByIssue = await LoadLabelsAsync(dbContext, issues, cancellationToken);

        return issues
            .Select(issue => new IssueSummaryResponse(
                issue.Id,
                issue.Identifier.Value,
                issue.Title,
                issue.Status.ToString(),
                issue.Priority.ToString(),
                issue.Estimate,
                ToUserResponse(issue.AssigneeId, users),
                labelsByIssue[issue.Id],
                issue.CreatedAt))
            .ToArray();
    }

    private static IssueResponse ToResponse(
        Issue issue,
        IReadOnlyDictionary<Guid, User> users,
        IReadOnlyList<LabelResponse> labels,
        IssueSprintResponse? sprint,
        IssueRoadmapItemResponse? roadmapItem) =>
        new(
            issue.Id,
            issue.Identifier.Value,
            issue.Title,
            issue.Description,
            issue.Status.ToString(),
            issue.Priority.ToString(),
            issue.Estimate,
            ToUserResponse(issue.AssigneeId, users),
            // Quien crea un issue no se elimina de la base (la aplicación no borra cuentas),
            // así que este usuario siempre está en el diccionario.
            ToUserResponse(issue.CreatedById, users)!,
            labels,
            sprint,
            roadmapItem,
            issue.CreatedAt,
            issue.UpdatedAt,
            issue.CompletedAt,
            issue.ArchivedAt);

    private static IssueUserResponse? ToUserResponse(Guid? userId, IReadOnlyDictionary<Guid, User> users)
    {
        if (userId is not { } id || !users.TryGetValue(id, out var user))
        {
            return null;
        }

        return new IssueUserResponse(user.Id, user.Name, user.AvatarUrl);
    }

    private static HashSet<Guid> CollectUserIds(IReadOnlyList<Issue> issues)
    {
        var userIds = new HashSet<Guid>();

        foreach (var issue in issues)
        {
            userIds.Add(issue.CreatedById);

            if (issue.AssigneeId is { } assigneeId)
            {
                userIds.Add(assigneeId);
            }
        }

        return userIds;
    }

    private static async Task<Dictionary<Guid, User>> LoadUsersAsync(
        AppDbContext dbContext,
        HashSet<Guid> userIds,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

    private static async Task<Dictionary<Guid, IReadOnlyList<LabelResponse>>> LoadLabelsAsync(
        AppDbContext dbContext,
        IReadOnlyList<Issue> issues,
        CancellationToken cancellationToken)
    {
        var issueIds = issues.Select(issue => issue.Id).ToArray();

        var rows = await dbContext.IssueLabels
            .AsNoTracking()
            .Where(issueLabel => issueIds.Contains(issueLabel.IssueId))
            .Join(
                dbContext.Labels.AsNoTracking(),
                issueLabel => issueLabel.LabelId,
                label => label.Id,
                (issueLabel, label) => new { issueLabel.IssueId, Label = label })
            .ToArrayAsync(cancellationToken);

        var labelsByIssue = rows
            .GroupBy(row => row.IssueId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<LabelResponse>)group
                    .Select(row => LabelResponseMapper.ToResponse(row.Label))
                    .ToArray());

        // Cada issue de la página aparece en el diccionario, tenga labels o no: evita que
        // el llamador tenga que manejar la ausencia de la clave como caso aparte.
        foreach (var issue in issues)
        {
            labelsByIssue.TryAdd(issue.Id, []);
        }

        return labelsByIssue;
    }
}
