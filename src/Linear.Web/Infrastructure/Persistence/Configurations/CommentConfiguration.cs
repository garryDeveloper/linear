using Linear.Domain.Comments;
using Linear.Domain.Issues;
using Linear.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .ValueGeneratedNever();

        builder.Property(comment => comment.IssueId).IsRequired();
        builder.Property(comment => comment.AuthorId).IsRequired();

        builder.Property(comment => comment.Content)
            .HasMaxLength(Comment.MaxContentLength)
            .IsRequired();

        builder.Property(comment => comment.CreatedAt).IsRequired();
        builder.Property(comment => comment.UpdatedAt).IsRequired();
        builder.Property(comment => comment.DeletedAt);

        // El listado siempre es "los comentarios de este issue, en orden cronológico":
        // el índice cubre el filtro y el orden de una sola vez.
        builder.HasIndex(comment => new { comment.IssueId, comment.CreatedAt });

        // Eliminar el issue se lleva su conversación: un comentario sin issue no tiene
        // dónde mostrarse.
        builder.HasOne<Issue>()
            .WithMany()
            .HasForeignKey(comment => comment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        // El autor se conserva igual que en Issues.CreatedById: perder la cuenta de alguien
        // no debería borrar lo que escribió ni dejar el comentario sin firma.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
