using Linear.Domain.Roadmaps;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class RoadmapItemConfiguration : IEntityTypeConfiguration<RoadmapItem>
{
    public void Configure(EntityTypeBuilder<RoadmapItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RoadmapItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedNever();

        builder.Property(item => item.RoadmapId).IsRequired();

        builder.Property(item => item.Name)
            .HasMaxLength(RoadmapItem.MaxNameLength)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasMaxLength(RoadmapItem.MaxDescriptionLength);

        // El estado se guarda como texto, igual que en el resto del modelo: la base sigue
        // siendo legible y reordenar el enum no reinterpreta los datos existentes.
        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.StartDate).IsRequired();
        builder.Property(item => item.TargetDate).IsRequired();

        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        // La línea de tiempo pide las iniciativas de un roadmap ordenadas por fecha.
        builder.HasIndex(item => new { item.RoadmapId, item.StartDate });
    }
}
