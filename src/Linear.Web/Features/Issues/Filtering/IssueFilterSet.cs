using Linear.Domain.Common;

namespace Linear.Web.Features.Issues.Filtering;

/// <summary>
/// Las condiciones activas de un listado, combinadas con Y.
/// </summary>
/// <remarks>
/// Como máximo una condición por campo: cada campo es un parámetro de la query string, y
/// eso es exactamente lo que muestra el constructor de filtros —una fila por campo—. Cubre
/// todos los ejemplos de la task; expresar dos condiciones sobre el mismo campo pediría una
/// gramática bastante más pesada, y ninguna pantalla la necesita.
/// </remarks>
public sealed class IssueFilterSet
{
    public static readonly IssueFilterSet Empty = new([]);

    private IssueFilterSet(IReadOnlyList<IssueFilter> filters) => Filters = filters;

    public IReadOnlyList<IssueFilter> Filters { get; }

    public bool IsEmpty => Filters.Count == 0;

    /// <summary>
    /// Arma el conjunto a partir de las expresiones crudas de cada campo. Las que vienen
    /// vacías o ausentes simplemente no aportan condición.
    /// </summary>
    public static Result<IssueFilterSet> Parse(IEnumerable<(IssueFilterField Field, string? Expression)> raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var filters = new List<IssueFilter>();

        foreach (var (field, expression) in raw)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                continue;
            }

            var filter = IssueFilter.Parse(field, expression);

            if (filter.IsFailure)
            {
                return Result.Failure<IssueFilterSet>(filter.Error);
            }

            filters.Add(filter.Value);
        }

        return Result.Success(new IssueFilterSet(filters));
    }

    /// <summary>
    /// Devuelve las condiciones como pares listos para una query string, en el orden en que
    /// se declaran los campos.
    /// </summary>
    public IEnumerable<(string Name, string Value)> ToQueryParameters() =>
        Filters
            .OrderBy(filter => filter.Field)
            .Select(filter => (filter.Field.ToQueryName(), filter.ToExpression()));
}
