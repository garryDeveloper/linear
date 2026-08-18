using Linear.Domain.Teams;
using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TeamMembers");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id)
            .ValueGeneratedNever();

        builder.Property(member => member.TeamId).IsRequired();
        builder.Property(member => member.UserId).IsRequired();

        // El rol se guarda como texto para que la base siga siendo legible y para que
        // reordenar el enum no reinterprete los datos existentes.
        builder.Property(member => member.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(member => member.JoinedAt).IsRequired();

        // Un usuario no puede pertenecer dos veces al mismo equipo.
        builder.HasIndex(member => new { member.TeamId, member.UserId })
            .IsUnique();

        // Acelera "los equipos del usuario", que es la consulta de arranque de la aplicación.
        builder.HasIndex(member => member.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
