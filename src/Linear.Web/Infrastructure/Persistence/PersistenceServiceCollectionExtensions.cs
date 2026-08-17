using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string ConnectionStringName = "Postgres";

    /// <summary>
    /// Registra el acceso a PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Falta de connection string es un error de despliegue, no una condición de negocio:
    /// se falla al arrancar en lugar de descubrirlo en el primer request.
    /// </remarks>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la connection string '{ConnectionStringName}'. " +
                $"Definila en appsettings o en la variable de entorno " +
                $"'ConnectionStrings__{ConnectionStringName}'.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }
}
