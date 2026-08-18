using System.Net;

using Linear.Domain.Users;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Infrastructure.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Linear.IntegrationTests.Features.Authentication;

/// <summary>
/// Verifica las políticas contra el contenedor real de la aplicación, y no contra una
/// configuración armada en el test: lo que interesa es que las políticas registradas en
/// <c>Program</c> se comporten como se espera.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthorizationPolicyTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public AuthorizationPolicyTests(PostgresFixture postgres)
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

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Member, false)]
    public async Task RequireAdmin_OnlyAdmitsAdministrators(UserRole role, bool expected)
    {
        var authorized = await AuthorizeAsync(AuthorizationPolicies.RequireAdmin, role);

        Assert.Equal(expected, authorized);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Member)]
    public async Task RequireMember_AdmitsAnyRecognizedRole(UserRole role)
    {
        Assert.True(await AuthorizeAsync(AuthorizationPolicies.RequireMember, role));
    }

    [Theory]
    [InlineData(AuthorizationPolicies.RequireAdmin)]
    [InlineData(AuthorizationPolicies.RequireMember)]
    public async Task NoPolicyAdmitsAnAnonymousUser(string policy)
    {
        using var scope = _factory.Services.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var anonymous = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());

        var result = await authorization.AuthorizeAsync(anonymous, resource: null, policy);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ThePagesRedirectToLoginPreservingTheDestination()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/settings", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = AuthenticationScenario.RedirectPath(response);

        Assert.Contains("/account/login", location, StringComparison.Ordinal);
        Assert.Contains("returnUrl", location, StringComparison.Ordinal);
        Assert.Contains("settings", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheLoginPageIsReachableWithoutASession()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/account/login", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnAuthenticatedUserReachesTheApplication()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, "ana@linear.dev");
        using var client = await AuthenticationScenario.SignInAsync(_factory, "ana@linear.dev");

        using var home = await client.GetAsync("/", CancellationToken.None);
        using var settings = await client.GetAsync("/settings", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal(HttpStatusCode.OK, settings.StatusCode);
    }


    [Fact]
    public async Task WithASession_AnUnknownApiRouteRespondsNotFoundWithoutHtml()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, "ana@linear.dev");
        using var client = await AuthenticationScenario.SignInAsync(_factory, "ana@linear.dev");

        using var response = await client.GetAsync("/api/does-not-exist", CancellationToken.None);

        // Con sesión válida ya no interviene la autorización: el 404 llega, y sigue siendo
        // JSON, sin reejecutar la página de error de la interfaz.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<bool> AuthorizeAsync(string policy, UserRole role)
    {
        using var scope = _factory.Services.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var user = User.Create(
            Email.Create("ana@linear.dev").Value,
            "Ana",
            role,
            "hash",
            Now).Value;

        var principal = UserClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme);

        var result = await authorization.AuthorizeAsync(principal, resource: null, policy);

        return result.Succeeded;
    }
}
