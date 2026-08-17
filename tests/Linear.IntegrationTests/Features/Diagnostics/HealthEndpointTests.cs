using System.Net;
using System.Net.Http.Json;

using Linear.Web.Features.Diagnostics.Health;

namespace Linear.IntegrationTests.Features.Diagnostics;

/// <summary>
/// Verifica el primer vertical slice de punta a punta: ruta, endpoint, handler,
/// resolución del <c>AppDbContext</c> y traducción de <c>Result</c> a HTTP.
/// </summary>
public class HealthEndpointTests(LinearWebApplicationFactory factory)
    : IClassFixture<LinearWebApplicationFactory>
{
    [Fact]
    public async Task Health_RespondsOk()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReportsTheEnvironmentAndTheStateOfTheDatabase()
    {
        using var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health", CancellationToken.None);

        Assert.NotNull(health);
        Assert.Equal("Testing", health.Environment);

        // La base configurada es inalcanzable, así que el estado tiene que ser degradado
        // en lugar de un error del endpoint: el diagnóstico sigue siendo legible.
        Assert.Equal(DatabaseStatuses.Unavailable, health.Database);
        Assert.Equal(HealthStatuses.Degraded, health.Status);
    }

    [Fact]
    public async Task Health_RespondsJson()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", CancellationToken.None);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AnUnknownApiRoute_RespondsNotFoundWithoutHtml()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
