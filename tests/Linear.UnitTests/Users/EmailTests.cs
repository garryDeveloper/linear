using Linear.Domain.Users;

namespace Linear.UnitTests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("ana@linear.dev")]
    [InlineData("ana.perez@linear.dev")]
    [InlineData("ana+tareas@linear.dev")]
    [InlineData("admin@linear.local")]
    public void ValidAddresses_AreAccepted(string value)
    {
        var email = Email.Create(value);

        Assert.True(email.IsSuccess);
        Assert.Equal(value, email.Value.Value);
    }

    [Theory]
    [InlineData("  Ana@Linear.Dev  ", "ana@linear.dev")]
    [InlineData("ANA@LINEAR.DEV", "ana@linear.dev")]
    public void TheAddressIsNormalized(string value, string expected)
    {
        var email = Email.Create(value);

        Assert.True(email.IsSuccess);
        Assert.Equal(expected, email.Value.Value);
    }

    [Fact]
    public void TwoAddressesThatDifferOnlyInCase_AreTheSame()
    {
        var first = Email.Create("Ana@Linear.dev").Value;
        var second = Email.Create("ana@LINEAR.dev").Value;

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyAddress_IsRejected(string? value)
    {
        var email = Email.Create(value);

        Assert.True(email.IsFailure);
        Assert.Equal(EmailErrors.Empty, email.Error);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("dos@arrobas@linear.dev")]
    [InlineData("@linear.dev")]
    [InlineData("ana@")]
    [InlineData("con espacio@linear.dev")]
    [InlineData("Ana Perez <ana@linear.dev>")]
    public void AMalformedAddress_IsRejected(string value)
    {
        var email = Email.Create(value);

        Assert.True(email.IsFailure);
        Assert.Equal(EmailErrors.InvalidFormat, email.Error);
    }

    [Fact]
    public void AnAddressLongerThanTheLimit_IsRejected()
    {
        var value = new string('a', Email.MaxLength) + "@linear.dev";

        var email = Email.Create(value);

        Assert.True(email.IsFailure);
        Assert.Equal(EmailErrors.TooLong, email.Error);
    }
}
