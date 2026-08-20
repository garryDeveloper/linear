using System.Reflection;

using Linear.Domain.Comments;
using Linear.Domain.Issues;
using Linear.Domain.Activities;
using Linear.Domain.Labels;
using Linear.Domain.Roadmaps;
using Linear.Domain.Sprints;
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

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Sprint> Sprints => Set<Sprint>();

    public DbSet<Roadmap> Roadmaps => Set<Roadmap>();

    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        IgnorePendingActivity(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saca del modelo la lista de actividad pendiente de los agregados.
    /// </summary>
    /// <remarks>
    /// Es estado en memoria que vive entre que el agregado registra lo que pasó y el
    /// interceptor lo persiste como <c>Activity</c>: no es una columna ni una relación. Sin
    /// esto, EF intenta mapearla y falla.
    ///
    /// Se hace acá y no en cada configuración para que alcance con implementar
    /// <see cref="IHasActivity"/>: un agregado nuevo no tiene que acordarse de ignorarla.
    /// </remarks>
    private static void IgnorePendingActivity(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.IsAssignableTo(typeof(IHasActivity)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Ignore(nameof(IHasActivity.PendingActivity));
            }
        }
    }

    /// <summary>
    /// Indica si la base de datos está accesible.
    /// </summary>
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        Database.CanConnectAsync(cancellationToken);
}
