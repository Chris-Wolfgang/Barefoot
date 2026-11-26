using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_recipient</c> table: one recipient a search mails its digest to (ADR 0006).
/// </summary>
internal sealed class SearchRecipientConfiguration : IEntityTypeConfiguration<SearchRecipient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchRecipient> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(link => link.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<SearchMatrix>()
            .WithMany()
            .HasForeignKey(link => link.SearchMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_search_recipient_recipient_id.
        // Serves: finding the link rows of one recipient, which deleting the recipient removes.
        builder
            .HasOne<Recipient>()
            .WithMany()
            .HasForeignKey(link => link.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading everything one search names, and the rule that a search names a row once.
        // It leads with the search, so it is also that foreign key's index.
        builder
            .HasIndex(link => new { link.SearchMatrixId, link.RecipientId })
            .IsUnique();
    }
}
