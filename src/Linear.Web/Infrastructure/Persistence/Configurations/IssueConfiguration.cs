using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Domain.Sprints;
using Linear.Domain.Teams;
using Linear.Domain.Users;

using Linear.Web.Infrastructure.Search;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NpgsqlTypes;

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

        // Sin límite de longitud: es contenido Markdown de formato libre.
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

        builder.Property(issue => issue.RoadmapItemId);

        // La línea de tiempo cuenta el avance de cada iniciativa por estado.
        builder.HasIndex(issue => new { issue.RoadmapItemId, issue.Status });

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

        // Mismo criterio con el roadmap: eliminar una iniciativa —o el roadmap entero—
        // desasocia sus issues, no los borra.
        builder.HasOne<RoadmapItem>()
            .WithMany()
            .HasForeignKey(issue => issue.RoadmapItemId)
            .OnDelete(DeleteBehavior.SetNull);

        ConfigureSearch(builder);

        builder.HasMany(issue => issue.Labels)
            .WithOne()
            .HasForeignKey(label => label.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Issue.Labels))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <summary>
    /// Columna de búsqueda de texto completo sobre el título y la descripción.
    /// </summary>
    /// <remarks>
    /// Es una propiedad sombra y no un campo de <see cref="Issue"/>: el dominio no depende
    /// de PostgreSQL (.ai/architecture.md), y un <c>tsvector</c> es un detalle del motor.
    ///
    /// La columna se genera y se guarda —<c>STORED</c>—, no se calcula en cada consulta:
    /// así el índice GIN puede apoyarse en ella. Eso exige que la expresión sea IMMUTABLE,
    /// y por eso la configuración de idioma va escrita como literal: <c>to_tsvector(texto)</c>
    /// con un solo argumento depende de un parámetro de sesión y no califica.
    ///
    /// El título pesa más que la descripción (<c>A</c> contra <c>B</c>) para que un issue
    /// que menciona el término en su título quede por encima de otro que solo lo menciona
    /// de pasada en el cuerpo.
    /// </remarks>
    private static void ConfigureSearch(EntityTypeBuilder<Issue> builder)
    {
        builder.Property<NpgsqlTsVector>(SearchSchema.SearchVectorColumn)
            .HasComputedColumnSql(
                $"""
                 setweight(to_tsvector('{SearchSchema.Configuration}', coalesce("Title", '')), 'A') ||
                 setweight(to_tsvector('{SearchSchema.Configuration}', coalesce("Description", '')), 'B')
                 """,
                stored: true);

        builder.HasIndex(SearchSchema.SearchVectorColumn)
            .HasMethod("GIN");
    }
}
