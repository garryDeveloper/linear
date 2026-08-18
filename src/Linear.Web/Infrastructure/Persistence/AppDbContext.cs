using System.Reflection;

using Linear.Domain.Teams;
using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia de la aplicación.
/// </summary>
/// <remarks>
/// Las configuraciones se descubren por reflexión desde este assembly, así que cada
/// feature aporta la suya sin tocar esta clase.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Team> Teams => Set<Team>();

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
