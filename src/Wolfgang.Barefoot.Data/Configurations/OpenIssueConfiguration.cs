using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>open_issue</c> table: what one person has been told needs their attention (DESIGN §12).
/// </summary>
internal sealed class OpenIssueConfiguration : IEntityTypeConfiguration<OpenIssue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OpenIssue> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(issue => issue.Id)
            .ValueGeneratedNever();

        builder
            .Property(issue => issue.Subject)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(issue => issue.ResolvesWhen)
            .HasMaxLength(ColumnLengths.Sentence);

        builder
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(issue => issue.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: listing one person's issues that are not resolved yet, which every page asks for.
        builder.HasIndex(issue => new { issue.UserId, issue.ResolvedUtc });
    }
}
