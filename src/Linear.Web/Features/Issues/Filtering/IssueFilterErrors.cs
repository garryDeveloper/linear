using Linear.Domain.Common;

namespace Linear.Web.Features.Issues.Filtering;

public static class IssueFilterErrors
{
    public static Error ValueRequired(IssueFilterField field) => Error.Validation(
        "Issues.Filter.ValueRequired",
        $"El filtro por {field.ToQueryName()} necesita al menos un valor.");

    public static Error SingleValueExpected(IssueFilterField field) => Error.Validation(
        "Issues.Filter.SingleValueExpected",
        $"El filtro por {field.ToQueryName()} con 'is' acepta un solo valor. Usá 'in' para varios.");

    public static Error OperatorNotSupported(IssueFilterField field, FilterOperator op) => Error.Validation(
        "Issues.Filter.OperatorNotSupported",
        $"El operador '{op}' no se puede usar con {field.ToQueryName()}.");

    public static Error UnknownOperator(string op) => Error.Validation(
        "Issues.Filter.UnknownOperator",
        $"'{op}' no es un operador válido. Los válidos son: is, isNot, in, notIn, contains.");

    public static Error UnknownValue(IssueFilterField field, string value) => Error.Validation(
        "Issues.Filter.UnknownValue",
        $"'{value}' no es un valor válido para el filtro por {field.ToQueryName()}.");

    public static readonly Error NoCurrentUser = Error.Unauthorized(
        "Issues.Filter.NoCurrentUser",
        "El filtro 'me' necesita una sesión iniciada.");
}
