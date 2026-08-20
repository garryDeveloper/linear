using Linear.Domain.Activities;
using Linear.Domain.Teams;
using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Activities");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id)
            .ValueGeneratedNever();

        builder.Property(activity => activity.TeamId).IsRequired();
        builder.Property(activity => activity.UserId).IsRequired();

        builder.Property(activity => activity.EntityType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(activity => activity.EntityId).IsRequired();

        // Como texto, igual que el resto de los enums del modelo: la base sigue siendo
        // legible, y sumar acciones nuevas no reinterpreta las viejas. Importa más que en
        // otras tablas, porque el historial no se puede reescribir.
        builder.Property(activity => activity.Action)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        // jsonb y no text: permite consultar dentro del payload, que es lo que hace posible
        // armar el historial de un issue incluyendo lo que pasó en sus comentarios.
        builder.Property(activity => activity.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(activity => activity.CreatedAt).IsRequired();

        // El feed del equipo: lo más reciente primero.
        builder.HasIndex(activity => new { activity.TeamId, activity.CreatedAt });

        // El historial de una entidad concreta.
        builder.HasIndex(activity => new { activity.EntityType, activity.EntityId });

        // El historial de un issue pregunta por el issueId que va dentro del payload, con el
        // operador de contención de jsonb: un índice GIN es lo que hace que esa pregunta no
        // recorra la tabla entera.
        builder.HasIndex(activity => activity.PayloadJson)
            .HasMethod("GIN");

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(activity => activity.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // El actor se conserva: un historial que pierde de vista quién hizo qué no sirve
        // para auditar nada.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
