using Linear.Domain.Roadmaps;
using Linear.Domain.Teams;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class RoadmapConfiguration : IEntityTypeConfiguration<Roadmap>
{
    public void Configure(EntityTypeBuilder<Roadmap> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Roadmaps");

        builder.HasKey(roadmap => roadmap.Id);

        builder.Property(roadmap => roadmap.Id)
            .ValueGeneratedNever();

        builder.Property(roadmap => roadmap.TeamId).IsRequired();

        builder.Property(roadmap => roadmap.Name)
            .HasMaxLength(Roadmap.MaxNameLength)
            .IsRequired();

        builder.Property(roadmap => roadmap.Description)
            .HasMaxLength(Roadmap.MaxDescriptionLength);

        builder.Property(roadmap => roadmap.CreatedAt).IsRequired();
        builder.Property(roadmap => roadmap.UpdatedAt).IsRequired();

        // El listado de roadmaps de un equipo.
        builder.HasIndex(roadmap => roadmap.TeamId);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(roadmap => roadmap.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Las iniciativas se cargan y se guardan con el roadmap: son parte de su agregado.
        builder.HasMany(roadmap => roadmap.Items)
            .WithOne()
            .HasForeignKey(item => item.RoadmapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Roadmap.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
