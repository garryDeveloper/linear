namespace Linear.Web.Features.Search.SearchIssues;

public sealed class SearchIssuesRequest
{
    /// <summary>Lo que se escribió en el buscador.</summary>
    public string? Query { get; set; }

    /// <summary>
    /// Cuántos resultados devolver como máximo. El buscador es una lista corta que se lee
    /// de un vistazo, no un listado paginado.
    /// </summary>
    public int Limit { get; set; } = SearchIssuesHandler.DefaultLimit;
}
