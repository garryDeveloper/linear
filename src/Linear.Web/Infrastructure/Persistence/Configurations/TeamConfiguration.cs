using Linear.Domain.Teams;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Teams");

        builder.HasKey(team => team.Id);

        builder.Property(team => team.Id)
            .ValueGeneratedNever();

        builder.Property(team => team.Name)
            .HasMaxLength(Team.MaxNameLength)
            .IsRequired();

        builder.Property(team => team.Key)
            .HasConversion(
                key => key.Value,
                value => TeamKey.FromPersistence(value))
            .HasMaxLength(TeamKey.MaxLength)
            .IsRequired();

        // La unicidad de la clave se garantiza en la base: dos peticiones simultáneas con
        // la misma clave pasarían ambas una comprobación previa en memoria.
        builder.HasIndex(team => team.Key)
            .IsUnique();

        builder.Property(team => team.Description)
            .HasMaxLength(Team.MaxDescriptionLength);

        builder.Property(team => team.CreatedAt).IsRequired();
        builder.Property(team => team.UpdatedAt).IsRequired();

        builder.Property(team => team.LastIssueNumber)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasMany(team => team.Members)
            .WithOne()
            .HasForeignKey(member => member.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Los miembros se manipulan solo a través del agregado, así que EF escribe y lee
        // directamente el campo de respaldo en lugar de la propiedad de solo lectura.
        builder.Metadata
            .FindNavigation(nameof(Team.Members))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
