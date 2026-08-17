using System.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia de la aplicación.
/// </summary>
/// <remarks>
/// Todavía no declara <see cref="DbSet{TEntity}"/> porque las entidades de negocio
/// están fuera del alcance de la task 001. Las configuraciones se descubren por
/// reflexión desde este assembly, así que cada feature podrá aportar la suya
/// sin tocar esta clase.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Indica si la base de datos está accesible.
    /// </summary>
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        Database.CanConnectAsync(cancellationToken);
}
