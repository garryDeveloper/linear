using Linear.Domain.Labels;
using Linear.Domain.Teams;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Labels");

        builder.HasKey(label => label.Id);

        builder.Property(label => label.Id)
            .ValueGeneratedNever();

        builder.Property(label => label.TeamId).IsRequired();

        builder.Property(label => label.Name)
            .HasMaxLength(Label.MaxNameLength)
            .IsRequired();

        builder.Property(label => label.NormalizedName)
            .HasMaxLength(Label.MaxNameLength)
            .IsRequired();

        // La unicidad se garantiza en la base: comprobar antes en memoria dejaría pasar
        // dos peticiones simultáneas con el mismo nombre.
        builder.HasIndex(label => new { label.TeamId, label.NormalizedName })
            .IsUnique();

        builder.Property(label => label.Description)
            .HasMaxLength(Label.MaxDescriptionLength);

        builder.Property(label => label.Color)
            .HasConversion(
                color => color.Value,
                value => LabelColor.FromPersistence(value))
            .HasMaxLength(LabelColor.Length)
            .IsRequired();

        builder.Property(label => label.CreatedAt).IsRequired();
        builder.Property(label => label.UpdatedAt).IsRequired();

        // Al eliminar un equipo se van sus labels: no tienen sentido fuera de él.
        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(label => label.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
