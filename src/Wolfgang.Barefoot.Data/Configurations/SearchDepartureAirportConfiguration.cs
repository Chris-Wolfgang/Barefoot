using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_departure_airport</c> table: one airport a search looks up flights from.
/// </summary>
internal sealed class SearchDepartureAirportConfiguration : IEntityTypeConfiguration<SearchDepartureAirport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchDepartureAirport> builder)
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

        // EF gives this foreign key its index, ix_search_departure_airport_airport_id.
        // Serves: finding the searches that depart from one airport, which the catalog asks when an airport
        // stops being a departure airport.
        builder
            .HasOne<Airport>()
            .WithMany()
            .HasForeignKey(link => link.AirportId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading everything one search names, and the rule that a search names a row once.
        // It leads with the search, so it is also that foreign key's index.
        builder
            .HasIndex(link => new { link.SearchMatrixId, link.AirportId })
            .IsUnique();
    }
}
