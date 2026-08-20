using Linear.Domain.Common;

namespace Linear.Web.Features.Issues.Filtering;

/// <summary>
/// Una condición del listado: un campo, un operador y sus valores todavía sin resolver.
/// </summary>
/// <remarks>
/// Los valores quedan como texto a propósito. Interpretarlos —"me" es un usuario, "bug" es
/// una label de este equipo— depende de quién consulta y de la base, así que es trabajo de
/// <see cref="IssueFilterQuery"/> y no del parseo de la URL.
/// </remarks>
public sealed record IssueFilter
{
    private const char ValueSeparator = ',';
    private const char OperatorSeparator = ':';

    private IssueFilter(IssueFilterField field, FilterOperator op, IReadOnlyList<string> values)
    {
        Field = field;
        Operator = op;
        Values = values;
    }

    public IssueFilterField Field { get; }

    public FilterOperator Operator { get; }

    public IReadOnlyList<string> Values { get; }

    /// <summary>
    /// Interpreta la expresión de un filtro tal como viaja en la query string:
    /// <c>[operador:]valor[,valor…]</c>.
    /// </summary>
    /// <remarks>
    /// El operador se puede omitir, y es lo habitual: sin prefijo, un valor es <c>is</c> y
    /// varios son <c>in</c> —que es como se leería en castellano—, y el prefijo <c>not:</c>
    /// los convierte en <c>isNot</c> y <c>notIn</c>. Los nombres largos se aceptan igual
    /// para poder escribir una URL explícita a mano.
    ///
    /// Un valor no puede contener una coma, porque la coma separa valores. Ninguno de los
    /// campos filtrables la necesita.
    /// </remarks>
    public static Result<IssueFilter> Parse(IssueFilterField field, string expression)
    {
        var (operatorToken, valuesToken) = SplitOperator(expression);

        var values = valuesToken
            .Split(ValueSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (values.Length == 0)
        {
            return Result.Failure<IssueFilter>(IssueFilterErrors.ValueRequired(field));
        }

        var op = ResolveOperator(field, operatorToken, values.Length);

        if (op.IsFailure)
        {
            return Result.Failure<IssueFilter>(op.Error);
        }

        var validation = Validate(field, op.Value, values.Length);

        return validation.IsFailure
            ? Result.Failure<IssueFilter>(validation.Error)
            : Result.Success(new IssueFilter(field, op.Value, values));
    }

    /// <summary>
    /// Vuelve a la expresión de la query string. Es la inversa de <see cref="Parse"/>: lo que
    /// se arma acá tiene que poder volver a leerse igual, que es lo que hace compartible una
    /// vista filtrada.
    /// </summary>
    public string ToExpression()
    {
        var values = string.Join(ValueSeparator, Values);

        // El prefijo solo hace falta para excluir: incluir es la lectura por omisión, y la
        // cantidad de valores ya distingue is de in.
        return Operator.IsNegated() ? $"not{OperatorSeparator}{values}" : values;
    }

    private static (string? Operator, string Values) SplitOperator(string expression)
    {
        var separator = expression.IndexOf(OperatorSeparator, StringComparison.Ordinal);

        return separator < 0
            ? (null, expression)
            : (expression[..separator].Trim(), expression[(separator + 1)..]);
    }

    private static Result<FilterOperator> ResolveOperator(
        IssueFilterField field,
        string? token,
        int valueCount)
    {
        if (string.IsNullOrEmpty(token))
        {
            // Sin prefijo: el campo de texto solo sabe hacer 'contains', y para el resto la
            // cantidad de valores decide entre 'is' e 'in'.
            return Result.Success(field.IsText()
                ? FilterOperator.Contains
                : valueCount == 1 ? FilterOperator.Is : FilterOperator.In);
        }

        return token.ToLowerInvariant() switch
        {
            "is" => Result.Success(FilterOperator.Is),
            "isnot" => Result.Success(FilterOperator.IsNot),
            "in" => Result.Success(FilterOperator.In),
            "notin" => Result.Success(FilterOperator.NotIn),
            "contains" => Result.Success(FilterOperator.Contains),
            // 'not' es la forma corta: significa isNot o notIn según cuántos valores haya.
            "not" => Result.Success(valueCount == 1 ? FilterOperator.IsNot : FilterOperator.NotIn),
            _ => Result.Failure<FilterOperator>(IssueFilterErrors.UnknownOperator(token))
        };
    }

    private static Result Validate(IssueFilterField field, FilterOperator op, int valueCount)
    {
        var textField = field.IsText();

        if (textField != (op == FilterOperator.Contains))
        {
            return Result.Failure(IssueFilterErrors.OperatorNotSupported(field, op));
        }

        return !op.AcceptsManyValues() && valueCount > 1
            ? Result.Failure(IssueFilterErrors.SingleValueExpected(field))
            : Result.Success();
    }
}
