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

    /// <summary>
    /// Sin sesión no se informa el entorno.
    /// </summary>
    /// <remarks>
    /// El endpoint es anónimo a propósito —una sonda pregunta antes de que exista cualquier
    /// sesión—, y saber que una instalación corre en Development es saber que tiene
    /// encendidos los errores detallados y el registro de datos sensibles. El estado, que es
    /// para lo que sirve la sonda, se informa igual.
    /// </remarks>
    [Fact]
    public async Task Health_DoesNotRevealTheEnvironmentToAnonymousCallers()
    {
        using var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health", CancellationToken.None);

        Assert.NotNull(health);
        Assert.Null(health.Environment);

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
    public async Task WithoutASession_AnUnknownApiRouteRespondsUnauthorized()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist", CancellationToken.None);

        // La política de respaldo también alcanza a los requests que no coinciden con
        // ningún endpoint, así que sin sesión el 401 llega antes que el 404. Es la
        // respuesta deseable: no revela qué rutas existen.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
