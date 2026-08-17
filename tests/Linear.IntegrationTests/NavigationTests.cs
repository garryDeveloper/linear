using System.Net;

namespace Linear.IntegrationTests;

/// <summary>
/// Verifica que la aplicación arranca y que la navegación básica responde.
/// </summary>
public class NavigationTests(LinearWebApplicationFactory factory)
    : IClassFixture<LinearWebApplicationFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/settings")]
    public async Task ThePagesOfTheApplicationRender(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TheHomePageRendersTheLayout()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/", CancellationToken.None);

        Assert.Contains("Linear", html, StringComparison.Ordinal);
        Assert.Contains("Estado del sistema", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownRouteRespondsNotFound()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/does-not-exist", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
