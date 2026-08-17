using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Construye el <see cref="AppDbContext"/> para las herramientas de línea de comandos
/// de EF Core (<c>dotnet ef migrations</c>, <c>dotnet ef database update</c>).
/// </summary>
/// <remarks>
/// Existe para que las migraciones no dependan de levantar la aplicación completa:
/// en tiempo de diseño no hay servidor, ni entorno de hosting, ni contenedor de servicios.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration
            .GetConnectionString(PersistenceServiceCollectionExtensions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la connection string " +
                $"'{PersistenceServiceCollectionExtensions.ConnectionStringName}' " +
                $"para ejecutar las herramientas de EF Core.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
