using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Linear.IntegrationTests;

/// <summary>
/// Levanta la aplicación completa en memoria.
/// </summary>
/// <remarks>
/// La connection string apunta a un puerto cerrado a propósito: la task 001 no incluye
/// tests contra una base real, y una dirección inalcanzable hace que el resultado sea
/// determinista tanto en una máquina con PostgreSQL corriendo como sin él.
/// El entorno propio evita además que se cargue <c>appsettings.Development.json</c>.
/// </remarks>
public sealed class LinearWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string UnreachableDatabaseConnectionString =
        "Host=127.0.0.1;Port=1;Database=linear_tests;Username=tests;Password=tests;Timeout=1;Command Timeout=1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", UnreachableDatabaseConnectionString);
        builder.UseSetting("Api:BaseAddress", "http://localhost/");
    }
}
