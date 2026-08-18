using Linear.Domain.Teams;

namespace Linear.UnitTests.Teams;

public class TeamKeyTests
{
    [Theory]
    [InlineData("WEB")]
    [InlineData("CORE")]
    [InlineData("MOBILE")]
    [InlineData("API2")]
    public void ValidKeysAreAccepted(string value)
    {
        var key = TeamKey.Create(value);

        Assert.True(key.IsSuccess);
        Assert.Equal(value, key.Value.Value);
    }

    [Theory]
    [InlineData("web", "WEB")]
    [InlineData("  core  ", "CORE")]
    [InlineData("MoBiLe", "MOBILE")]
    public void TheKeyIsNormalizedToUppercase(string value, string expected)
    {
        var key = TeamKey.Create(value);

        Assert.Equal(expected, key.Value.Value);
    }

    [Fact]
    public void TwoKeysThatDifferOnlyInCaseAreTheSame()
    {
        Assert.Equal(TeamKey.Create("web").Value, TeamKey.Create("WEB").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyKeyIsRejected(string? value)
    {
        var key = TeamKey.Create(value);

        Assert.True(key.IsFailure);
        Assert.Equal(TeamKeyErrors.Empty, key.Error);
    }

    [Theory]
    [InlineData("W")]
    [InlineData("DEMASIADOLARGO")]
    public void AKeyOutsideTheAllowedLengthIsRejected(string value)
    {
        var key = TeamKey.Create(value);

        Assert.True(key.IsFailure);
        Assert.Equal(TeamKeyErrors.InvalidLength, key.Error);
    }

    [Theory]
    [InlineData("2WEB")]
    [InlineData("WEB-1")]
    [InlineData("WEB_1")]
    [InlineData("WE B")]
    [InlineData("WEBÑ")]
    public void AMalformedKeyIsRejected(string value)
    {
        var key = TeamKey.Create(value);

        Assert.True(key.IsFailure);
        Assert.Equal(TeamKeyErrors.InvalidFormat, key.Error);
    }
}
