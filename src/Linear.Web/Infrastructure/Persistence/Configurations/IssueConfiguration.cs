using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Domain.Teams;
using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Issues");

        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.Id)
            .ValueGeneratedNever();

        builder.Property(issue => issue.Identifier)
            .HasConversion(
                identifier => identifier.Value,
                value => IssueIdentifier.FromPersistence(value))
            .HasMaxLength(32)
            .IsRequired();

        // Único a nivel global y no solo por equipo: el identificador ya incorpora la
        // clave del equipo, así que dos equipos nunca compiten por el mismo valor.
        builder.HasIndex(issue => issue.Identifier)
            .IsUnique();

        builder.Property(issue => issue.TeamId).IsRequired();

        // El listado de issues siempre filtra por equipo; agrupar además por estado es el
        // comportamiento por omisión de la lista, así que el índice cubre las dos.
        builder.HasIndex(issue => new { issue.TeamId, issue.Status });

        builder.Property(issue => issue.Title)
            .HasMaxLength(Issue.MaxTitleLength)
            .IsRequired();

        // Sin límite de longitud: es contenido Markdown de formato libre (Task 012).
        builder.Property(issue => issue.Description);

        builder.Property(issue => issue.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(issue => issue.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(issue => issue.Estimate);

        builder.Property(issue => issue.AssigneeId);
        builder.HasIndex(issue => issue.AssigneeId);

        builder.Property(issue => issue.SprintId);

        // El tablero de un sprint pide sus issues agrupados por estado.
        builder.HasIndex(issue => new { issue.SprintId, issue.Status });

        builder.Property(issue => issue.CreatedById).IsRequired();

        builder.Property(issue => issue.CreatedAt).IsRequired();
        builder.Property(issue => issue.UpdatedAt).IsRequired();
        builder.Property(issue => issue.CompletedAt);
        builder.Property(issue => issue.ArchivedAt);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(issue => issue.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ningún usuario se elimina hoy —solo se desactiva—, pero de nulificar en lugar de
        // arrastrar el borrado: perder la cuenta de alguien nunca debería arrastrar sus issues.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(issue => issue.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(issue => issue.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Si alguna vez desapareciera un sprint, sus issues vuelven a quedar sin sprint en
        // lugar de irse con él: el trabajo sobrevive a la planificación que lo agrupaba.
        builder.HasOne<Sprint>()
            .WithMany()
            .HasForeignKey(issue => issue.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(issue => issue.Labels)
            .WithOne()
            .HasForeignKey(label => label.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Issue.Labels))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
