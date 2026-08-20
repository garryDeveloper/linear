namespace Linear.Web.Features.Issues.Filtering;

/// <summary>
/// Campos por los que se puede filtrar el listado de issues.
/// </summary>
/// <remarks>
/// Es un conjunto cerrado y no un campo libre: cada uno se traduce a una condición SQL
/// escrita a mano, así que agregar un filtro es agregar código, no aceptar cualquier
/// nombre que llegue por la URL.
/// </remarks>
public enum IssueFilterField
{
    Status = 0,
    Priority = 1,
    Assignee = 2,
    Label = 3,
    Sprint = 4,
    CreatedBy = 5,

    /// <summary>
    /// Título del issue. Es el único campo de texto, y por eso el único que acepta
    /// <see cref="FilterOperator.Contains"/>.
    /// </summary>
    /// <remarks>
    /// Es una coincidencia parcial simple, no la búsqueda de la task 009: esa recorre
    /// también descripción y comentarios con Full Text Search. Acá alcanza con acotar un
    /// listado que ya está filtrado por equipo.
    /// </remarks>
    Title = 6
}

public static class IssueFilterFieldExtensions
{
    /// <summary>Nombre del campo tal como viaja en la query string.</summary>
    public static string ToQueryName(this IssueFilterField field) =>
        char.ToLowerInvariant(field.ToString()[0]) + field.ToString()[1..];

    /// <summary>Indica si el campo es de texto libre.</summary>
    public static bool IsText(this IssueFilterField field) => field == IssueFilterField.Title;
}
