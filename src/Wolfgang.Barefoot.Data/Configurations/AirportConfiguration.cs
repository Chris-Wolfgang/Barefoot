using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>airport</c> table: an airport the booking site knows (DESIGN §7).
/// </summary>
internal sealed class AirportConfiguration : IEntityTypeConfiguration<Airport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Airport> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(airport => airport.Id)
            .ValueGeneratedNever();

        builder
            .Property(airport => airport.Code)
            .HasMaxLength(ColumnLengths.AirportCode);

        builder
            .Property(airport => airport.Name)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(airport => airport.City)
            .HasMaxLength(ColumnLengths.PlaceName);

        builder
            .Property(airport => airport.State)
            .HasMaxLength(ColumnLengths.PlaceName);

        builder
            .Property(airport => airport.Country)
            .HasMaxLength(ColumnLengths.PlaceName);

        builder
            .Property(airport => airport.TimeZoneId)
            .HasMaxLength(ColumnLengths.TimeZoneId);

        builder
            .Property(airport => airport.Version)
            .IsConcurrencyToken();

        // Serves: finding an airport by its code, which a catalog snapshot does for every airport it
        // lists, and the rule that a code is one airport.
        builder
            .HasIndex(airport => airport.Code)
            .IsUnique();
    }
}
