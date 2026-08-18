using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasConversion(
                email => email.Value,
                value => Email.FromPersistence(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        // El email identifica al usuario en el login: la unicidad se garantiza en la base,
        // que es el único lugar donde no puede colarse una condición de carrera.
        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.Name)
            .HasMaxLength(User.MaxNameLength)
            .IsRequired();

        builder.Property(user => user.AvatarUrl)
            .HasMaxLength(User.MaxAvatarUrlLength);

        // El rol se guarda como texto para que la base siga siendo legible y para que
        // reordenar el enum no reinterprete los datos existentes.
        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .IsRequired();
    }
}
