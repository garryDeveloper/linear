using System.Security.Claims;

using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;

namespace Linear.UnitTests.Authentication;

public class ClaimsTests
{
    private const string Scheme = "TestScheme";

    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private static User AUser(UserRole role = UserRole.Member, string? avatarUrl = null) =>
        User.Create(Email.Create("ana@linear.dev").Value, "Ana Pérez", role, "hash", Now, avatarUrl).Value;

    [Fact]
    public void ThePrincipalCarriesTheIdentityOfTheUser()
    {
        var user = AUser(UserRole.Admin, "https://linear.dev/ana.png");

        var principal = UserClaims.CreatePrincipal(user, Scheme);

        Assert.True(principal.IsAuthenticated());
        Assert.Equal(user.Id, principal.GetUserId());
        Assert.Equal("Ana Pérez", principal.GetName());
        Assert.Equal("ana@linear.dev", principal.GetEmail());
        Assert.Equal(UserRole.Admin, principal.GetRole());
        Assert.Equal("https://linear.dev/ana.png", principal.GetAvatarUrl());
    }

    [Fact]
    public void WithoutAnAvatarTheClaimIsNotEmitted()
    {
        var principal = UserClaims.CreatePrincipal(AUser(), Scheme);

        Assert.Null(principal.GetAvatarUrl());
    }

    [Fact]
    public void ThePrincipalCarriesTheRoleInTheStandardClaim()
    {
        // La política RequireAdmin se evalúa sobre ClaimTypes.Role: si el claim cambiara
        // de nombre, la autorización dejaría de funcionar en silencio.
        var principal = UserClaims.CreatePrincipal(AUser(UserRole.Admin), Scheme);

        Assert.True(principal.IsInRole(nameof(UserRole.Admin)));
        Assert.False(principal.IsInRole(nameof(UserRole.Member)));
    }

    [Fact]
    public void ThePasswordHashNeverTravelsInTheClaims()
    {
        var user = AUser();

        var principal = UserClaims.CreatePrincipal(user, Scheme);

        Assert.DoesNotContain(principal.Claims, claim =>
            claim.Value.Contains(user.PasswordHash, StringComparison.Ordinal));
    }

    [Fact]
    public void AnAnonymousPrincipalHasNoIdentity()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(principal.IsAuthenticated());
        Assert.Null(principal.GetUserId());
        Assert.Null(principal.GetRole());
    }

    [Fact]
    public void AnUnknownRoleIsNotInterpreted()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "SuperAdmin")], Scheme);

        Assert.Null(new ClaimsPrincipal(identity).GetRole());
    }
}
