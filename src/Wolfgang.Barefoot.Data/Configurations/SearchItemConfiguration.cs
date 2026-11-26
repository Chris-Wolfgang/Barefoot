using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_item</c> table: the link that says one search wants one lookup in one run (ADR 0004 §2).
/// </summary>
internal sealed class SearchItemConfiguration : IEntityTypeConfiguration<SearchItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchItem> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(item => item.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<MatrixRun>()
            .WithMany()
            .HasForeignKey(item => item.MatrixRunId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_search_item_search_request_id.
        // Serves: finding the searches that want one lookup, which storing its result asks for.
        builder
            .HasOne<SearchRequest>()
            .WithMany()
            .HasForeignKey(item => item.SearchRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the lookups one search wants in a run, and the rule that a search links to
        // a lookup once. It leads with the search's run, so it is also that foreign key's index.
        builder
            .HasIndex(item => new { item.MatrixRunId, item.SearchRequestId })
            .IsUnique();
    }
}
