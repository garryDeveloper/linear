using System.Net;

namespace Linear.IntegrationTests;

/// <summary>
/// Verifica que la aplicación arranca y cómo responde a quien no inició sesión.
/// </summary>
/// <remarks>
/// No necesita base de datos: ninguno de estos caminos llega a consultar usuarios.
/// La navegación con sesión iniciada se cubre en los tests de autorización.
/// </remarks>
public class NavigationTests(LinearWebApplicationFactory factory)
    : IClassFixture<LinearWebApplicationFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/settings")]
    [InlineData("/account/profile")]
    public async Task WithoutASession_ThePagesSendToLogin(string path)
    {
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains(
            "/account/login",
            response.Headers.Location?.OriginalString ?? string.Empty,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheLoginPageRendersItsForm()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/account/login", CancellationToken.None);

        Assert.Contains("Input.Email", html, StringComparison.Ordinal);
        Assert.Contains("Input.Password", html, StringComparison.Ordinal);

        // Sin token antiforgery, el formulario sería vulnerable a un envío desde otro sitio.
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheLoginPageDoesNotNeedTheDatabase()
    {
        // La factory apunta a una base inalcanzable: la pantalla de login tiene que poder
        // mostrarse igual, porque es la única vía de entrada a la aplicación.
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/account/login", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
