using Linear.Web.Features.Issues.Filtering;

namespace Linear.UnitTests.Issues;

/// <summary>
/// Interpretación de las expresiones de filtro tal como llegan en la query string.
/// </summary>
public class IssueFilterTests
{
    [Fact]
    public void OneValueWithoutAPrefixIsTheIsOperator()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "InProgress");

        Assert.True(filter.IsSuccess);
        Assert.Equal(FilterOperator.Is, filter.Value.Operator);
        Assert.Equal(["InProgress"], filter.Value.Values);
    }

    [Fact]
    public void SeveralValuesWithoutAPrefixAreTheInOperator()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Priority, "High,Urgent");

        Assert.True(filter.IsSuccess);
        Assert.Equal(FilterOperator.In, filter.Value.Operator);
        Assert.Equal(["High", "Urgent"], filter.Value.Values);
    }

    [Fact]
    public void TheNotPrefixWithOneValueIsTheIsNotOperator()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "not:Done");

        Assert.True(filter.IsSuccess);
        Assert.Equal(FilterOperator.IsNot, filter.Value.Operator);
        Assert.Equal(["Done"], filter.Value.Values);
    }

    [Fact]
    public void TheNotPrefixWithSeveralValuesIsTheNotInOperator()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "not:Done,Canceled");

        Assert.True(filter.IsSuccess);
        Assert.Equal(FilterOperator.NotIn, filter.Value.Operator);
        Assert.Equal(["Done", "Canceled"], filter.Value.Values);
    }

    [Fact]
    public void ATextFieldWithoutAPrefixIsTheContainsOperator()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Title, "login");

        Assert.True(filter.IsSuccess);
        Assert.Equal(FilterOperator.Contains, filter.Value.Operator);
        Assert.Equal(["login"], filter.Value.Values);
    }

    [Theory]
    [InlineData("is:InProgress", FilterOperator.Is)]
    [InlineData("isNot:InProgress", FilterOperator.IsNot)]
    [InlineData("in:InProgress", FilterOperator.In)]
    [InlineData("notIn:InProgress", FilterOperator.NotIn)]
    public void TheLongOperatorNamesAreAccepted(string expression, FilterOperator expected)
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, expression);

        Assert.True(filter.IsSuccess);
        Assert.Equal(expected, filter.Value.Operator);
    }

    [Theory]
    [InlineData("IS:InProgress")]
    [InlineData("NotIn:InProgress")]
    [InlineData("nOtIn:InProgress")]
    public void OperatorNamesDoNotDependOnCasing(string expression)
    {
        Assert.True(IssueFilter.Parse(IssueFilterField.Status, expression).IsSuccess);
    }

    [Fact]
    public void ValuesAreTrimmedAndEmptyOnesAreDropped()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Priority, "in:  High , , Urgent  ");

        Assert.Equal(["High", "Urgent"], filter.Value.Values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("in:")]
    [InlineData("not: , ")]
    public void AnExpressionWithoutValuesIsRejected(string expression)
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, expression);

        Assert.True(filter.IsFailure);
        Assert.Equal(IssueFilterErrors.ValueRequired(IssueFilterField.Status), filter.Error);
    }

    [Fact]
    public void AnUnknownOperatorIsRejected()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "between:A,B");

        Assert.True(filter.IsFailure);
        Assert.Equal(IssueFilterErrors.UnknownOperator("between"), filter.Error);
    }

    [Fact]
    public void IsWithSeveralValuesIsRejected()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "is:Todo,Done");

        Assert.True(filter.IsFailure);
        Assert.Equal(IssueFilterErrors.SingleValueExpected(IssueFilterField.Status), filter.Error);
    }

    /// <summary>"contains" es solo para texto, y el texto solo sabe hacer "contains".</summary>
    [Fact]
    public void ContainsIsRejectedOnANonTextField()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Status, "contains:Prog");

        Assert.True(filter.IsFailure);
        Assert.Equal(
            IssueFilterErrors.OperatorNotSupported(IssueFilterField.Status, FilterOperator.Contains),
            filter.Error);
    }

    [Fact]
    public void TheComparisonOperatorsAreRejectedOnATextField()
    {
        var filter = IssueFilter.Parse(IssueFilterField.Title, "is:login");

        Assert.True(filter.IsFailure);
        Assert.Equal(
            IssueFilterErrors.OperatorNotSupported(IssueFilterField.Title, FilterOperator.Is),
            filter.Error);
    }

    /// <summary>
    /// Lo que se escribe en la URL tiene que poder volver a leerse igual: es lo que hace
    /// compartible una vista filtrada.
    /// </summary>
    [Theory]
    [InlineData(IssueFilterField.Status, "InProgress", "InProgress")]
    [InlineData(IssueFilterField.Status, "not:Done", "not:Done")]
    [InlineData(IssueFilterField.Priority, "High,Urgent", "High,Urgent")]
    [InlineData(IssueFilterField.Priority, "not:Low,Medium", "not:Low,Medium")]
    [InlineData(IssueFilterField.Title, "login", "login")]
    // Las formas largas se normalizan a la corta, que significa lo mismo.
    [InlineData(IssueFilterField.Status, "is:Done", "Done")]
    [InlineData(IssueFilterField.Status, "isNot:Done", "not:Done")]
    [InlineData(IssueFilterField.Status, "in:Todo,Done", "Todo,Done")]
    [InlineData(IssueFilterField.Status, "notIn:Todo,Done", "not:Todo,Done")]
    public void AnExpressionRoundTrips(IssueFilterField field, string expression, string expected)
    {
        var filter = IssueFilter.Parse(field, expression);

        Assert.Equal(expected, filter.Value.ToExpression());

        // Y lo que sale se vuelve a interpretar igual.
        var again = IssueFilter.Parse(field, filter.Value.ToExpression());

        Assert.Equal(filter.Value.Operator, again.Value.Operator);
        Assert.Equal(filter.Value.Values, again.Value.Values);
    }

    [Fact]
    public void TheQueryNameOfAFieldIsItsCamelCaseName()
    {
        Assert.Equal("status", IssueFilterField.Status.ToQueryName());
        Assert.Equal("createdBy", IssueFilterField.CreatedBy.ToQueryName());
    }

    [Fact]
    public void OnlyTheTitleIsATextField()
    {
        Assert.True(IssueFilterField.Title.IsText());

        foreach (var field in Enum.GetValues<IssueFilterField>().Where(f => f != IssueFilterField.Title))
        {
            Assert.False(field.IsText());
        }
    }

    [Theory]
    [InlineData(FilterOperator.Is, false)]
    [InlineData(FilterOperator.In, false)]
    [InlineData(FilterOperator.Contains, false)]
    [InlineData(FilterOperator.IsNot, true)]
    [InlineData(FilterOperator.NotIn, true)]
    public void OnlyIsNotAndNotInExclude(FilterOperator op, bool expected)
    {
        Assert.Equal(expected, op.IsNegated());
    }
}

/// <summary>
/// Armado del conjunto de condiciones a partir de los parámetros de la URL.
/// </summary>
public class IssueFilterSetTests
{
    [Fact]
    public void AnEmptySetHasNoConditions()
    {
        var set = IssueFilterSet.Parse(
        [
            (IssueFilterField.Status, null),
            (IssueFilterField.Priority, "   ")
        ]);

        Assert.True(set.IsSuccess);
        Assert.True(set.Value.IsEmpty);
    }

    [Fact]
    public void SeveralFieldsCombine()
    {
        var set = IssueFilterSet.Parse(
        [
            (IssueFilterField.Status, "InProgress"),
            (IssueFilterField.Priority, "High,Urgent"),
            (IssueFilterField.Assignee, "me")
        ]);

        Assert.True(set.IsSuccess);
        Assert.Equal(3, set.Value.Filters.Count);
    }

    [Fact]
    public void TheFirstInvalidExpressionStopsTheParsing()
    {
        var set = IssueFilterSet.Parse(
        [
            (IssueFilterField.Status, "InProgress"),
            (IssueFilterField.Priority, "between:A,B")
        ]);

        Assert.True(set.IsFailure);
        Assert.Equal(IssueFilterErrors.UnknownOperator("between"), set.Error);
    }

    [Fact]
    public void TheSetGoesBackToQueryParameters()
    {
        var set = IssueFilterSet.Parse(
        [
            (IssueFilterField.Priority, "not:Low"),
            (IssueFilterField.Status, "Todo,InProgress")
        ]);

        Assert.Equal(
            [("status", "Todo,InProgress"), ("priority", "not:Low")],
            set.Value.ToQueryParameters());
    }
}
