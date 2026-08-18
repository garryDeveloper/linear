using Linear.Web.Infrastructure.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Linear.IntegrationTests.Infrastructure;

/// <summary>
/// Aplicación completa apuntando a la base de datos de tests.
/// </summary>
public sealed class DatabaseWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", connectionString);

        // Cada test crea los usuarios que necesita: la cuenta sembrada agregaría filas
        // inesperadas a las verificaciones.
        builder.UseSetting("Seed:Enabled", "false");

        // El servidor de test responde sobre HTTP: con la cookie marcada Secure, el cliente
        // no la guardaría y ningún test podría verificar una sesión.
        builder.UseSetting(AppAuthenticationExtensions.RequireHttpsConfigurationKey, "false");
    }
}
