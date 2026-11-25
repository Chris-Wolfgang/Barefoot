using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>flight_leg</c> table: one flight of a direction, as the site gave it.
/// </summary>
internal sealed class FlightLegConfiguration : IEntityTypeConfiguration<FlightLeg>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FlightLeg> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(leg => leg.Id)
            .ValueGeneratedNever();

        builder
            .Property(leg => leg.Carrier)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(leg => leg.FlightNumber)
            .HasMaxLength(ColumnLengths.FlightCode);

        builder
            .Property(leg => leg.OperatingCarrier)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(leg => leg.CabinName)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(leg => leg.FareCode)
            .HasMaxLength(ColumnLengths.FlightCode);

        // The two airports are stored as the site's codes, with no foreign key: a leg may stop at an
        // airport the catalog has never listed.
        builder
            .Property(leg => leg.DepartureAirportCode)
            .HasMaxLength(ColumnLengths.AirportCode);

        builder
            .Property(leg => leg.ArrivalAirportCode)
            .HasMaxLength(ColumnLengths.AirportCode);

        builder
            .HasOne<FlightDirection>()
            .WithMany()
            .HasForeignKey(leg => leg.FlightDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the flights of one direction in order, and the rule that a place in the
        // direction is taken once. It leads with the direction, so it is also the foreign key's index.
        builder
            .HasIndex(leg => new { leg.FlightDirectionId, leg.Sequence })
            .IsUnique();
    }
}
