using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Infrastructure;

/// <summary>
/// Base de datos dedicada a los tests, creada con las mismas migraciones que produccción.
/// </summary>
/// <remarks>
/// Se usa PostgreSQL real y no un proveedor en memoria: índices únicos, conversiones de
/// value objects y tipos como <c>timestamptz</c> solo se comportan igual contra el motor
/// verdadero, y son justamente las piezas que sostienen la unicidad del email.
/// Requiere el contenedor de <c>docker compose</c> levantado.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=linear_tests;Username=linear;Password=linear_dev;Timeout=5";

    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("LINEAR_TEST_POSTGRES") ?? DefaultConnectionString;

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();

        try
        {
            // Se parte de cero en cada corrida: una base arrastrada de una ejecución
            // anterior escondería migraciones que no saben aplicarse sobre datos vacíos.
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "No se pudo preparar la base de datos de tests. " +
                "Levantá PostgreSQL con 'docker compose up -d' antes de correr los tests.",
                exception);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Deja la base vacía para que cada test parta de un estado conocido.</summary>
    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "Users", "Teams", "Labels" CASCADE;""");
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
}

/// <summary>
/// Comparte una única base entre todas las clases de test que la necesitan: crearla y
/// migrarla por clase multiplicaría el costo sin agregar aislamiento.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
