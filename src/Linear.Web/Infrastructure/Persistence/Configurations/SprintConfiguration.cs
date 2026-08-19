using Linear.Domain.Sprints;
using Linear.Domain.Teams;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    /// <summary>
    /// Nombre del índice que sostiene "un solo sprint activo por equipo". Lo comparte el
    /// handler de inicio para reconocer la violación de unicidad cuando dos pedidos
    /// concurrentes intentan arrancar sprints distintos a la vez.
    /// </summary>
    public const string OneActiveSprintPerTeamIndex = "IX_Sprints_TeamId_Active";

    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Sprints");

        builder.HasKey(sprint => sprint.Id);

        builder.Property(sprint => sprint.Id)
            .ValueGeneratedNever();

        builder.Property(sprint => sprint.TeamId).IsRequired();

        builder.Property(sprint => sprint.Name)
            .HasMaxLength(Sprint.MaxNameLength)
            .IsRequired();

        builder.Property(sprint => sprint.Goal)
            .HasMaxLength(Sprint.MaxGoalLength);

        // DateOnly se mapea a 'date': sin hora y sin huso, que es exactamente lo que un
        // sprint necesita.
        builder.Property(sprint => sprint.StartDate).IsRequired();
        builder.Property(sprint => sprint.EndDate).IsRequired();

        builder.Property(sprint => sprint.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(sprint => sprint.CreatedAt).IsRequired();
        builder.Property(sprint => sprint.UpdatedAt).IsRequired();
        builder.Property(sprint => sprint.CompletedAt);

        // El listado de sprints de un equipo, del más nuevo al más viejo.
        builder.HasIndex(sprint => new { sprint.TeamId, sprint.StartDate });

        // "Solo puede existir un Sprint Active por Team" (task 007). Un chequeo en el
        // handler no alcanza: entre leer "no hay ninguno activo" y guardar el propio hay
        // una ventana en la que otro pedido puede hacer lo mismo, y los dos ganarían.
        // Un índice único parcial lo vuelve imposible sin necesidad de bloquear el equipo:
        // la base solo admite una fila Active por TeamId, y las demás filas —planificadas,
        // completadas, canceladas— quedan fuera del índice y no compiten entre sí.
        builder.HasIndex(sprint => sprint.TeamId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(SprintStatus.Active)}'")
            .HasDatabaseName(OneActiveSprintPerTeamIndex);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(sprint => sprint.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
