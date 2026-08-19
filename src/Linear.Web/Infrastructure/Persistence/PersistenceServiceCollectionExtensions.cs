using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string ConnectionStringName = "Postgres";

    /// <summary>
    /// Registra el acceso a PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Se expone una fábrica y no un <see cref="AppDbContext"/> compartido porque en Blazor
    /// Server el ámbito de inyección es el circuito completo, que dura toda la sesión del
    /// usuario. Un contexto por circuito significa que dos componentes que cargan datos a la
    /// vez usan la misma instancia —EF Core lo rechaza— y que el rastreador de cambios crece
    /// sin límite mientras la pestaña siga abierta. Cada operación crea el suyo y lo descarta.
    /// <para>
    /// El registro scoped adicional cubre el código que sí vive dentro de un request HTTP,
    /// donde el ámbito coincide con la operación.
    /// </para>
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

        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped(provider => provider
            .GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContext());

        return services;
    }
}
