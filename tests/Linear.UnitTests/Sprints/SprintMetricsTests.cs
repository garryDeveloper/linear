using Linear.Web.Features.Sprints.Contracts;

namespace Linear.UnitTests.Sprints;

/// <summary>
/// Las métricas que la task 007 pide mostrar: total, completados, pendientes y porcentaje.
/// </summary>
public class SprintMetricsTests
{
    [Fact]
    public void RemainingIsWhatIsLeftOfTheTotal()
    {
        var metrics = SprintMetrics.Create(total: 10, completed: 4);

        Assert.Equal(10, metrics.Total);
        Assert.Equal(4, metrics.Completed);
        Assert.Equal(6, metrics.Remaining);
        Assert.Equal(40, metrics.CompletionPercentage);
    }

    /// <summary>Un sprint sin issues es 0%, no una división por cero.</summary>
    [Fact]
    public void AnEmptySprintIsZeroPercent()
    {
        var metrics = SprintMetrics.Create(total: 0, completed: 0);

        Assert.Equal(0, metrics.Total);
        Assert.Equal(0, metrics.Remaining);
        Assert.Equal(0, metrics.CompletionPercentage);
    }

    [Fact]
    public void AFullyCompletedSprintIsAHundredPercent()
    {
        var metrics = SprintMetrics.Create(total: 7, completed: 7);

        Assert.Equal(0, metrics.Remaining);
        Assert.Equal(100, metrics.CompletionPercentage);
    }

    [Theory]
    [InlineData(3, 1, 33)]
    [InlineData(3, 2, 67)]
    [InlineData(8, 1, 13)]
    [InlineData(6, 5, 83)]
    public void ThePercentageIsRounded(int total, int completed, int expected)
    {
        Assert.Equal(expected, SprintMetrics.Create(total, completed).CompletionPercentage);
    }

    [Fact]
    public void EmptyIsTheSameAsZeroOverZero()
    {
        Assert.Equal(SprintMetrics.Create(0, 0), SprintMetrics.Empty);
    }
}
