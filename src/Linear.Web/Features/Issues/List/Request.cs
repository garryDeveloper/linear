using Linear.Web.Features.Issues.Filtering;
using Linear.Web.Shared.Pagination;

namespace Linear.Web.Features.Issues.List;

/// <summary>
/// Parámetros del listado de issues.
/// </summary>
/// <remarks>
/// Cada filtro es un parámetro suelto de la query string —<c>?status=InProgress&amp;
/// priority=in:High,Urgent</c>— y no un objeto anidado: así la URL queda legible, se puede
/// escribir a mano y compartir, que es lo que pide la task 008. El valor de cada uno es la
/// expresión cruda que interpreta <see cref="IssueFilter"/>.
/// </remarks>
public sealed class ListIssuesRequest
{
    public string Key { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    /// <summary>
    /// Si es <c>false</c> (el valor por omisión), los issues archivados quedan afuera. No es
    /// un filtro en el sentido de la task 008 —no tiene operadores ni se combina con los
    /// demás—, es el criterio base de qué cuenta como "la lista de issues" del equipo.
    /// </summary>
    public bool IncludeArchived { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    /// <summary>Acepta identificadores de usuario, <c>me</c> y <c>none</c> (sin responsable).</summary>
    public string? Assignee { get; set; }

    /// <summary>Acepta identificadores de label y también sus nombres.</summary>
    public string? Label { get; set; }

    /// <summary>Acepta identificadores de sprint y <c>none</c> (sin sprint).</summary>
    public string? Sprint { get; set; }

    /// <summary>Acepta identificadores de usuario y <c>me</c>.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Acepta identificadores de iniciativa y <c>none</c> (sin iniciativa).</summary>
    public string? RoadmapItem { get; set; }

    /// <summary>Coincidencia parcial en el título, sin distinguir mayúsculas.</summary>
    public string? Title { get; set; }

    public PageRequest ToPageRequest() => new() { Page = Page, PageSize = PageSize };

    /// <summary>Las expresiones de filtro tal como llegaron, listas para interpretar.</summary>
    public IEnumerable<(IssueFilterField Field, string? Expression)> FilterExpressions() =>
    [
        (IssueFilterField.Status, Status),
        (IssueFilterField.Priority, Priority),
        (IssueFilterField.Assignee, Assignee),
        (IssueFilterField.Label, Label),
        (IssueFilterField.Sprint, Sprint),
        (IssueFilterField.CreatedBy, CreatedBy),
        (IssueFilterField.RoadmapItem, RoadmapItem),
        (IssueFilterField.Title, Title)
    ];
}
