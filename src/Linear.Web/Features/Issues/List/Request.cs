using Linear.Web.Shared.Pagination;

namespace Linear.Web.Features.Issues.List;

public sealed class ListIssuesRequest
{
    public string Key { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    /// <summary>
    /// Si es <c>false</c> (el valor por omisión), los issues archivados quedan afuera. No es
    /// un filtro en el sentido de la task 008 —no hay operadores ni se puede combinar—, es
    /// el criterio base de qué cuenta como "la lista de issues" del equipo.
    /// </summary>
    public bool IncludeArchived { get; set; }

    public PageRequest ToPageRequest() => new() { Page = Page, PageSize = PageSize };
}
