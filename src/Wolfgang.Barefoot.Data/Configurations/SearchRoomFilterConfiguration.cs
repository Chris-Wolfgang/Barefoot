using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_room_filter</c> table: one vocabulary value a search filters rooms by.
/// </summary>
internal sealed class SearchRoomFilterConfiguration : IEntityTypeConfiguration<SearchRoomFilter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchRoomFilter> builder)
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

        // EF gives this foreign key its index, ix_search_room_filter_vocabulary_value_id.
        // Serves: finding the searches that filter by one value, which greying a value no result shows asks.
        builder
            .HasOne<VocabularyValue>()
            .WithMany()
            .HasForeignKey(link => link.VocabularyValueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading everything one search names, and the rule that a search names a row once.
        // It leads with the search, so it is also that foreign key's index.
        builder
            .HasIndex(link => new { link.SearchMatrixId, link.VocabularyValueId })
            .IsUnique();
    }
}
