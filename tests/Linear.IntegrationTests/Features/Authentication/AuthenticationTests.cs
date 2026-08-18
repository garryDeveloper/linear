using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Users;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Authentication.GetCurrentUser;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Authentication;

[Collection(PostgresCollection.Name)]
public sealed class AuthenticationTests : IAsyncLifetime
{
    private const string MemberEmail = "ana@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public AuthenticationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _factory = new DatabaseWebApplicationFactory(postgres.ConnectionString);
    }

    public Task InitializeAsync() => _postgres.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WithValidCredentials_TheSessionIsCreated()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(
            client, MemberEmail, AuthenticationScenario.DefaultPassword);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/", AuthenticationScenario.RedirectPath(response));
        Assert.True(AuthenticationScenario.HasSessionCookie(response));
    }

    [Fact]
    public async Task TheEmailIsNotCaseSensitive()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(
            client, "ANA@Linear.DEV", AuthenticationScenario.DefaultPassword);

        Assert.True(AuthenticationScenario.HasSessionCookie(response));
    }

    [Fact]
    public async Task WithAWrongPassword_NoSessionIsCreated()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(client, MemberEmail, "otra-cosa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(AuthenticationScenario.HasSessionCookie(response));
        Assert.Contains(
            UserErrors.InvalidCredentials.Description,
            await AuthenticationScenario.ReadHtmlAsync(response),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownEmailAndAWrongPassword_ReportTheSameThing()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        using var firstClient = AuthenticationScenario.CreateClient(_factory);
        using var wrongPassword = await AuthenticationScenario.PostLoginAsync(firstClient, MemberEmail, "otra-cosa");

        using var secondClient = AuthenticationScenario.CreateClient(_factory);
        using var unknownEmail = await AuthenticationScenario.PostLoginAsync(
            secondClient, "nadie@linear.dev", AuthenticationScenario.DefaultPassword);

        // Si los mensajes difirieran, cualquiera podría averiguar qué direcciones existen.
        Assert.Contains(
            UserErrors.InvalidCredentials.Description,
            await AuthenticationScenario.ReadHtmlAsync(wrongPassword),
            StringComparison.Ordinal);
        Assert.Contains(
            UserErrors.InvalidCredentials.Description,
            await AuthenticationScenario.ReadHtmlAsync(unknownEmail),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ADeactivatedAccount_CannotSignIn()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail, isActive: false);
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(
            client, MemberEmail, AuthenticationScenario.DefaultPassword);

        Assert.False(AuthenticationScenario.HasSessionCookie(response));
        Assert.Contains(
            UserErrors.Inactive.Description,
            await AuthenticationScenario.ReadHtmlAsync(response),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAuthenticatedUser_CanReadTheirIdentity()
    {
        var user = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail, UserRole.Admin);
        using var client = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        var current = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me", CancellationToken.None);

        Assert.NotNull(current);
        Assert.Equal(user.Id, current.Id);
        Assert.Equal(MemberEmail, current.Email);
        Assert.Equal(nameof(UserRole.Admin), current.Role);
    }

    [Fact]
    public async Task WithoutASession_TheApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/auth/me", CancellationToken.None);

        // 401 y no una redirección al login: un cliente que espera JSON no sabría qué
        // hacer con el HTML de una pantalla de inicio de sesión.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AfterSigningOut_TheSessionIsGone()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        using var logout = await AuthenticationScenario.PostLogoutAsync(client);

        Assert.True(AuthenticationScenario.ClearsSessionCookie(logout));

        using var afterLogout = await client.GetAsync("/api/auth/me", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task DeactivatingAnAccount_ClosesItsOpenSession()
    {
        var user = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        using var beforeDeactivation = await client.GetAsync("/api/auth/me", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, beforeDeactivation.StatusCode);

        await AuthenticationScenario.DeactivateUserAsync(_factory, user.Id);

        // La cookie sigue siendo criptográficamente válida: es la revalidación contra la
        // base la que tiene que rechazarla.
        using var afterDeactivation = await client.GetAsync("/api/auth/me", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task ChangingTheRole_RefreshesTheOpenSession()
    {
        var user = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
        using var client = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await dbContext.Users.FirstAsync(candidate => candidate.Id == user.Id);

            stored.ChangeRole(UserRole.Admin, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var current = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me", CancellationToken.None);

        Assert.Equal(nameof(UserRole.Admin), current!.Role);
    }
}
