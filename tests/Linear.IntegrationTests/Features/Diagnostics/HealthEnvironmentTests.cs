using System.Net.Http.Json;

using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Diagnostics.Health;

namespace Linear.IntegrationTests.Features.Diagnostics;

/// <summary>
/// A quién se le informa el entorno de hosting.
/// </summary>
/// <remarks>
/// <c>/api/health</c> responde sin autenticación, porque una sonda de disponibilidad tiene
/// que poder preguntar antes de que exista cualquier sesión. Eso lo vuelve el único endpoint
/// desde el que alguien de afuera podría averiguar algo, así que el dato sensible —el
/// entorno— se reserva para quien sí la tiene.
/// <para>
/// Estos tests van contra la base real y no contra la fábrica sin base de
/// <c>HealthEndpointTests</c>, porque hace falta un usuario con el que iniciar sesión.
/// </para>
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class HealthEnvironmentTests : IAsyncLifetime
{
    private const string Email = "sonda@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public HealthEnvironmentTests(PostgresFixture postgres)
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
    public async Task AnonymouslyTheEnvironmentIsNotReported()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Null(health.Environment);
    }

    /// <summary>
    /// Lo que la sonda necesita se responde igual sin sesión: ocultar el entorno no puede
    /// dejar el endpoint sin su función.
    /// </summary>
    [Fact]
    public async Task AnonymouslyTheStateIsStillReported()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Equal(HealthStatuses.Healthy, health.Status);
        Assert.Equal(DatabaseStatuses.Connected, health.Database);
        Assert.NotEqual(default, health.TimestampUtc);
    }

    [Fact]
    public async Task WithASessionTheEnvironmentIsReported()
    {
        await AuthenticationScenario.CreateUserAsync(_factory, Email);

        using var client = await AuthenticationScenario.SignInAsync(_factory, Email);

        var health = await client.GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.NotNull(health);
        Assert.Equal("Testing", health.Environment);
    }
}
