using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;

namespace Linear.UnitTests.Authentication;

public class PasswordHasherTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly AspNetPasswordHasher _hasher = new();

    private static User AUser() =>
        User.Create(Email.Create("ana@linear.dev").Value, "Ana", UserRole.Member, "pendiente", Now).Value;

    [Fact]
    public void TheHashIsNotThePassword()
    {
        var user = AUser();

        var hash = _hasher.Hash(user, "Linear-1234");

        Assert.NotEqual("Linear-1234", hash);
        Assert.DoesNotContain("Linear-1234", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSamePasswordProducesDifferentHashes()
    {
        var user = AUser();

        // Cada hash lleva su propia sal: dos usuarios con la misma contraseña no deben
        // ser identificables comparando la columna.
        Assert.NotEqual(_hasher.Hash(user, "Linear-1234"), _hasher.Hash(user, "Linear-1234"));
    }

    [Fact]
    public void TheCorrectPasswordIsVerified()
    {
        var user = AUser();
        user.ChangePasswordHash(_hasher.Hash(user, "Linear-1234"), Now);

        Assert.Equal(PasswordVerification.Success, _hasher.Verify(user, "Linear-1234"));
    }

    [Theory]
    [InlineData("otra-cosa")]
    [InlineData("linear-1234")]
    [InlineData("")]
    public void AnIncorrectPasswordIsRejected(string password)
    {
        var user = AUser();
        user.ChangePasswordHash(_hasher.Hash(user, "Linear-1234"), Now);

        Assert.Equal(PasswordVerification.Failed, _hasher.Verify(user, password));
    }

    [Fact]
    public void TheHashFitsInTheColumn()
    {
        var user = AUser();

        Assert.True(_hasher.Hash(user, "Linear-1234").Length <= 512);
    }

    [Fact]
    public void VerifyingAgainstTheDummyHashDoesNotThrow()
    {
        _hasher.VerifyDummy("cualquier-cosa");
        _hasher.VerifyDummy(string.Empty);
    }
}
