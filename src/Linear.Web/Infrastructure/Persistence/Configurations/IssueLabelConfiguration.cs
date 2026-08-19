using Linear.Domain.Issues;
using Linear.Domain.Labels;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Persistence.Configurations;

public sealed class IssueLabelConfiguration : IEntityTypeConfiguration<IssueLabel>
{
    public void Configure(EntityTypeBuilder<IssueLabel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("IssueLabels");

        builder.HasKey(issueLabel => new { issueLabel.IssueId, issueLabel.LabelId });

        // Borrar la label la quita de todos los issues que la tenían: no tiene sentido
        // conservar una asociación a una label que ya no existe.
        builder.HasOne<Label>()
            .WithMany()
            .HasForeignKey(issueLabel => issueLabel.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(issueLabel => issueLabel.LabelId);
    }
}
