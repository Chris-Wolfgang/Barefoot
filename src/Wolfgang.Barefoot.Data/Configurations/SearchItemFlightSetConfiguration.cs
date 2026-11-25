using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_item_flight_set</c> table: the link that says which flight lookup serves one search
/// item (ADR 0004 §6).
/// </summary>
internal sealed class SearchItemFlightSetConfiguration : IEntityTypeConfiguration<SearchItemFlightSet>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchItemFlightSet> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(link => link.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<SearchItem>()
            .WithMany()
            .HasForeignKey(link => link.SearchItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_search_item_flight_set_flight_set_id.
        // Serves: finding the search items one flight lookup serves, which storing its result asks for.
        builder
            .HasOne<FlightSet>()
            .WithMany()
            .HasForeignKey(link => link.FlightSetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the flight lookups of one search item, and the rule that an item links to
        // a lookup once. It leads with the item, so it is also that foreign key's index.
        builder
            .HasIndex(link => new { link.SearchItemId, link.FlightSetId })
            .IsUnique();
    }
}
