using Linear.Domain.Issues;
using Linear.Domain.Teams;

namespace Linear.UnitTests.Issues;

public class IssueIdentifierTests
{
    private static TeamKey AKey() => TeamKey.Create("WEB").Value;

    [Fact]
    public void TheIdentifierCombinesTheTeamKeyAndTheNumber()
    {
        var identifier = IssueIdentifier.Create(AKey(), 42);

        Assert.Equal("WEB-42", identifier.Value);
    }

    [Fact]
    public void TheFirstNumberIsOne()
    {
        var identifier = IssueIdentifier.Create(AKey(), 1);

        Assert.Equal("WEB-1", identifier.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ANumberBelowOneIsRejected(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IssueIdentifier.Create(AKey(), number));
    }

    [Fact]
    public void TwoIdentifiersWithTheSameValueAreEqual()
    {
        Assert.Equal(IssueIdentifier.Create(AKey(), 7), IssueIdentifier.Create(AKey(), 7));
    }

    [Fact]
    public void FromPersistenceDoesNotReformat()
    {
        Assert.Equal("CORE-3", IssueIdentifier.FromPersistence("CORE-3").Value);
    }
}
