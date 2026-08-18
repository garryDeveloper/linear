using Linear.Domain.Users;

namespace Linear.UnitTests.Users;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private static Email AnEmail() => Email.Create("ana@linear.dev").Value;

    private static User AUser() =>
        User.Create(AnEmail(), "Ana Pérez", UserRole.Member, "hash", Now).Value;

    [Fact]
    public void ANewUser_IsActiveAndKeepsItsData()
    {
        var user = User.Create(AnEmail(), "Ana Pérez", UserRole.Admin, "hash", Now);

        Assert.True(user.IsSuccess);
        Assert.Equal("Ana Pérez", user.Value.Name);
        Assert.Equal(UserRole.Admin, user.Value.Role);
        Assert.True(user.Value.IsActive);
        Assert.Equal(Now, user.Value.CreatedAt);
        Assert.Equal(Now, user.Value.UpdatedAt);
        Assert.NotEqual(Guid.Empty, user.Value.Id);
    }

    [Fact]
    public void TheNameIsTrimmed()
    {
        var user = User.Create(AnEmail(), "  Ana Pérez  ", UserRole.Member, "hash", Now);

        Assert.Equal("Ana Pérez", user.Value.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AUserWithoutAName_IsRejected(string name)
    {
        var user = User.Create(AnEmail(), name, UserRole.Member, "hash", Now);

        Assert.True(user.IsFailure);
        Assert.Equal(UserErrors.NameRequired, user.Error);
    }

    [Fact]
    public void ANameLongerThanTheLimit_IsRejected()
    {
        var user = User.Create(AnEmail(), new string('a', User.MaxNameLength + 1), UserRole.Member, "hash", Now);

        Assert.True(user.IsFailure);
        Assert.Equal(UserErrors.NameTooLong, user.Error);
    }

    [Fact]
    public void AUserWithoutAPasswordHash_IsRejected()
    {
        var user = User.Create(AnEmail(), "Ana", UserRole.Member, "  ", Now);

        Assert.True(user.IsFailure);
        Assert.Equal(UserErrors.PasswordHashRequired, user.Error);
    }

    [Fact]
    public void TheFirstFailedRuleIsTheOneReported()
    {
        // Nombre vacío y hash vacío a la vez: se informa el nombre, que es la primera regla.
        var user = User.Create(AnEmail(), "", UserRole.Member, "", Now);

        Assert.Equal(UserErrors.NameRequired, user.Error);
    }

    [Fact]
    public void AnEmptyAvatarUrl_IsStoredAsNull()
    {
        var user = User.Create(AnEmail(), "Ana", UserRole.Member, "hash", Now, avatarUrl: "   ");

        Assert.Null(user.Value.AvatarUrl);
    }

    [Fact]
    public void Deactivating_MarksTheUserAndUpdatesTheTimestamp()
    {
        var user = AUser();
        var later = Now.AddHours(1);

        user.Deactivate(later);

        Assert.False(user.IsActive);
        Assert.Equal(later, user.UpdatedAt);
    }

    [Fact]
    public void DeactivatingAnAlreadyInactiveUser_DoesNotTouchTheTimestamp()
    {
        var user = AUser();
        user.Deactivate(Now.AddHours(1));

        user.Deactivate(Now.AddHours(2));

        Assert.Equal(Now.AddHours(1), user.UpdatedAt);
    }

    [Fact]
    public void ChangingTheRole_UpdatesTheTimestamp()
    {
        var user = AUser();
        var later = Now.AddHours(1);

        user.ChangeRole(UserRole.Admin, later);

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal(later, user.UpdatedAt);
    }

    [Fact]
    public void ChangingToTheSameRole_DoesNotTouchTheTimestamp()
    {
        var user = AUser();

        user.ChangeRole(UserRole.Member, Now.AddHours(1));

        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void Renaming_ValidatesTheNewName()
    {
        var user = AUser();

        var result = user.Rename("", Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("Ana Pérez", user.Name);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void ChangingThePasswordHash_ReplacesItAndUpdatesTheTimestamp()
    {
        var user = AUser();
        var later = Now.AddHours(1);

        var result = user.ChangePasswordHash("nuevo-hash", later);

        Assert.True(result.IsSuccess);
        Assert.Equal("nuevo-hash", user.PasswordHash);
        Assert.Equal(later, user.UpdatedAt);
    }
}
